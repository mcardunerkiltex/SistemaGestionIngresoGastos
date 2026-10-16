using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gastos.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Gastos.IntegrationTests;

public sealed class AccountsAndCategoriesTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private sealed record Account(Guid Id, string Name, string Type, decimal InitialBalance, decimal Balance, bool IsArchived);
    private sealed record Category(Guid Id, Guid? ParentId, string Name, string Type, string? Icon, string? Color, List<Category> Children);

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public async Task New_user_starts_with_cash_account_and_category_tree()
    {
        var (client, _) = await factory.CreateUserAsync();

        var accounts = await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json);
        var expenses = await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Expense", Json);
        var incomes = await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Income", Json);

        var cash = Assert.Single(accounts!);
        Assert.Equal(("Efectivo", "Cash", 0m), (cash.Name, cash.Type, cash.Balance));
        Assert.Contains(expenses!, c => c.Name == "Comida" && c.Children.Any(s => s.Name == "Supermercado"));
        Assert.All(expenses!, c => Assert.Null(c.ParentId));
        Assert.Contains(incomes!, c => c.Name == "Sueldo");
    }

    [Fact]
    public async Task Account_balance_is_initial_balance_plus_movements()
    {
        var (client, userId) = await factory.CreateUserAsync();
        var created = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Banco", type = "Bank", initialBalance = 1000m }, Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var bank = (await created.Content.ReadFromJsonAsync<Account>(Json))!;
        var cash = (await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!.Single(a => a.Name == "Efectivo");

        await using (var db = factory.DbAs(userId))
        {
            var food = await db.Categories.FirstAsync(c => c.Name == "Supermercado");
            var salary = await db.Categories.FirstAsync(c => c.Name == "Sueldo");
            db.Transactions.Add(Transaction.Income(userId, bank.Id, salary.Id, 500m, Today));
            db.Transactions.Add(Transaction.Expense(userId, bank.Id, food.Id, 120.50m, Today));
            var (outRow, inRow) = Transaction.Transfer(userId, bank.Id, cash.Id, 200m, Today);
            db.Transactions.AddRange(outRow, inRow);
            await db.SaveChangesAsync();
        }

        var accounts = (await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!;
        Assert.Equal(1179.50m, accounts.Single(a => a.Id == bank.Id).Balance);
        Assert.Equal(200m, accounts.Single(a => a.Id == cash.Id).Balance);
    }

    [Fact]
    public async Task Account_with_movements_can_be_archived_but_not_deleted()
    {
        var (client, userId) = await factory.CreateUserAsync();
        var cash = (await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!.Single();
        await using (var db = factory.DbAs(userId))
        {
            var food = await db.Categories.FirstAsync(c => c.Name == "Supermercado");
            db.Transactions.Add(Transaction.Expense(userId, cash.Id, food.Id, 10m, Today));
            await db.SaveChangesAsync();
        }

        var delete = await client.DeleteAsync($"/api/v1/accounts/{cash.Id}");
        var archive = await client.PutAsJsonAsync($"/api/v1/accounts/{cash.Id}", new { name = "Efectivo", type = "Cash", initialBalance = 0m, isArchived = true }, Json);

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!);
        Assert.Single((await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts?includeArchived=true", Json))!);
    }

    [Fact]
    public async Task Empty_account_can_be_deleted()
    {
        var (client, _) = await factory.CreateUserAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Mercado Pago", type = "Wallet", initialBalance = 0m }, Json))
            .Content.ReadFromJsonAsync<Account>(Json);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/accounts/{created!.Id}")).StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts?includeArchived=true", Json))!, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Subcategory_inherits_parent_style_and_three_levels_are_rejected()
    {
        var (client, _) = await factory.CreateUserAsync();
        var food = (await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Expense", Json))!.Single(c => c.Name == "Comida");

        var created = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Carnicería", type = "Expense", parentId = food.Id }, Json);
        var child = (await created.Content.ReadFromJsonAsync<Category>(Json))!;
        var grandchild = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Pollo", type = "Expense", parentId = child.Id }, Json);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal((food.Id, food.Icon, food.Color), (child.ParentId!.Value, child.Icon, child.Color));
        Assert.Equal(HttpStatusCode.BadRequest, grandchild.StatusCode);
    }

    [Fact]
    public async Task Category_with_subcategories_or_movements_cannot_be_deleted()
    {
        var (client, userId) = await factory.CreateUserAsync();
        var tree = (await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Expense", Json))!;
        var food = tree.Single(c => c.Name == "Comida");
        var market = food.Children.Single(c => c.Name == "Supermercado");
        var delivery = food.Children.Single(c => c.Name == "Delivery");
        await using (var db = factory.DbAs(userId))
        {
            var cash = await db.Accounts.FirstAsync();
            db.Transactions.Add(Transaction.Expense(userId, cash.Id, market.Id, 10m, Today));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/categories/{food.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/categories/{market.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/categories/{delivery.Id}")).StatusCode);
    }

    [Fact]
    public async Task Users_cannot_touch_each_others_accounts_or_categories()
    {
        var (alice, _) = await factory.CreateUserAsync();
        var (bob, _) = await factory.CreateUserAsync();
        var aliceCash = (await alice.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!.Single();
        var aliceFood = (await alice.GetFromJsonAsync<List<Category>>("/api/v1/categories", Json))!.First();

        var edit = await bob.PutAsJsonAsync($"/api/v1/accounts/{aliceCash.Id}", new { name = "Mío", type = "Cash", initialBalance = 0m }, Json);
        var delete = await bob.DeleteAsync($"/api/v1/categories/{aliceFood.Id}");
        var subcategory = await bob.PostAsJsonAsync("/api/v1/categories", new { name = "X", type = "Expense", parentId = aliceFood.Id }, Json);

        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, subcategory.StatusCode);
    }

    [Fact]
    public async Task Invalid_input_returns_field_errors()
    {
        var (client, _) = await factory.CreateUserAsync();

        var account = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "", type = "Cash", initialBalance = 0m }, Json);
        var category = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Ok", type = "Expense", color = "rojo" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, account.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, category.StatusCode);
        Assert.Contains("color", await category.Content.ReadAsStringAsync());
    }
}
