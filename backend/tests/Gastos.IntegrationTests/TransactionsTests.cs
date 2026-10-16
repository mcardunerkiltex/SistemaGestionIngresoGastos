using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gastos.IntegrationTests;

public sealed class TransactionsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private sealed record Ref(Guid Id, string Name);
    private sealed record CategoryRef(Guid Id, string Name, string? ParentName, string? Icon, string? Color);
    private sealed record Movement(Guid Id, string Kind, decimal Amount, DateOnly Date, string? Note, Ref Account, CategoryRef? Category, Ref? ToAccount);
    private sealed record MovementPage(List<Movement> Items, int Total, int Page, int PageSize, decimal TotalIncome, decimal TotalExpense);
    private sealed record Account(Guid Id, string Name, decimal Balance);
    private sealed record Category(Guid Id, string Name, List<Category> Children);

    private sealed class Setup
    {
        public required HttpClient Client { get; init; }
        public required Guid Cash { get; init; }
        public required Guid Bank { get; init; }
        public required Guid Food { get; init; }
        public required Guid Market { get; init; }
        public required Guid Delivery { get; init; }
        public required Guid Salary { get; init; }
    }

    private async Task<Setup> SetupAsync()
    {
        var (client, _) = await factory.CreateUserAsync();
        var bank = await (await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Banco", type = "Bank", initialBalance = 0m }, Json))
            .Content.ReadFromJsonAsync<Account>(Json);
        var cash = (await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!.Single(a => a.Name == "Efectivo");
        var expenses = (await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Expense", Json))!;
        var incomes = (await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Income", Json))!;
        var food = expenses.Single(c => c.Name == "Comida");
        return new Setup
        {
            Client = client,
            Cash = cash.Id,
            Bank = bank!.Id,
            Food = food.Id,
            Market = food.Children.Single(c => c.Name == "Supermercado").Id,
            Delivery = food.Children.Single(c => c.Name == "Delivery").Id,
            Salary = incomes.Single(c => c.Name == "Sueldo").Id,
        };
    }

    private static async Task<Movement> PostAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Movement>(Json))!;
    }

    private static Task<Movement> Expense(Setup s, decimal amount, string date, Guid? category = null, Guid? account = null, string? note = null) =>
        PostAsync(s.Client, "/api/v1/transactions", new { kind = "Expense", accountId = account ?? s.Cash, categoryId = category ?? s.Market, amount, date, note });

    private static async Task<Dictionary<Guid, decimal>> BalancesAsync(Setup s) =>
        (await s.Client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!.ToDictionary(a => a.Id, a => a.Balance);

    [Fact]
    public async Task Expense_is_created_with_its_category_path_and_updates_the_balance()
    {
        var s = await SetupAsync();

        var created = await Expense(s, 1234.567m, "2026-10-16", note: "  Coto  ");

        Assert.Equal(("Expense", 1234.57m, "Coto"), (created.Kind, created.Amount, created.Note));
        Assert.Equal(("Supermercado", "Comida"), (created.Category!.Name, created.Category.ParentName));
        Assert.Equal(-1234.57m, (await BalancesAsync(s))[s.Cash]);
    }

    [Fact]
    public async Task Category_must_match_the_kind_of_movement()
    {
        var s = await SetupAsync();

        var response = await s.Client.PostAsJsonAsync("/api/v1/transactions",
            new { kind = "Income", accountId = s.Cash, categoryId = s.Market, amount = 10m, date = "2026-10-16" }, Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("categoría de ingresos", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Invalid_amount_and_archived_account_are_rejected()
    {
        var s = await SetupAsync();
        await s.Client.PutAsJsonAsync($"/api/v1/accounts/{s.Bank}", new { name = "Banco", type = "Bank", initialBalance = 0m, isArchived = true }, Json);

        var zero = await s.Client.PostAsJsonAsync("/api/v1/transactions", new { kind = "Expense", accountId = s.Cash, categoryId = s.Market, amount = 0m, date = "2026-10-16" }, Json);
        var archived = await s.Client.PostAsJsonAsync("/api/v1/transactions", new { kind = "Expense", accountId = s.Bank, categoryId = s.Market, amount = 5m, date = "2026-10-16" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, archived.StatusCode);
    }

    [Fact]
    public async Task Transfer_moves_money_between_accounts_and_lists_as_one_row()
    {
        var s = await SetupAsync();

        var transfer = await PostAsync(s.Client, "/api/v1/transactions/transfers",
            new { fromAccountId = s.Bank, toAccountId = s.Cash, amount = 300m, date = "2026-10-16", note = "Cajero" });

        Assert.Equal(("Transfer", s.Bank, s.Cash), (transfer.Kind, transfer.Account.Id, transfer.ToAccount!.Id));
        var balances = await BalancesAsync(s);
        Assert.Equal((-300m, 300m), (balances[s.Bank], balances[s.Cash]));

        var all = await s.Client.GetFromJsonAsync<MovementPage>("/api/v1/transactions", Json);
        var forCash = await s.Client.GetFromJsonAsync<MovementPage>($"/api/v1/transactions?accountId={s.Cash}", Json);
        Assert.Single(all!.Items);
        var incoming = Assert.Single(forCash!.Items);
        Assert.Equal((s.Bank, s.Cash), (incoming.Account.Id, incoming.ToAccount!.Id));
        Assert.Equal((0m, 0m), (all.TotalIncome, all.TotalExpense));
    }

    [Fact]
    public async Task Editing_a_transfer_updates_both_legs_and_deleting_removes_both()
    {
        var s = await SetupAsync();
        var transfer = await PostAsync(s.Client, "/api/v1/transactions/transfers",
            new { fromAccountId = s.Bank, toAccountId = s.Cash, amount = 300m, date = "2026-10-16" });

        var edit = await s.Client.PutAsJsonAsync($"/api/v1/transactions/transfers/{transfer.Id}",
            new { fromAccountId = s.Cash, toAccountId = s.Bank, amount = 50m, date = "2026-10-15" }, Json);
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var balances = await BalancesAsync(s);
        Assert.Equal((50m, -50m), (balances[s.Bank], balances[s.Cash]));

        Assert.Equal(HttpStatusCode.NoContent, (await s.Client.DeleteAsync($"/api/v1/transactions/{transfer.Id}")).StatusCode);
        balances = await BalancesAsync(s);
        Assert.Equal((0m, 0m), (balances[s.Bank], balances[s.Cash]));
    }

    [Fact]
    public async Task List_filters_by_month_category_tree_kind_and_note_with_totals()
    {
        var s = await SetupAsync();
        await Expense(s, 100m, "2026-10-01", s.Market, note: "Coto");
        await Expense(s, 50m, "2026-10-20", s.Delivery, note: "PedidosYa");
        await Expense(s, 70m, "2026-09-30", s.Market);
        await PostAsync(s.Client, "/api/v1/transactions", new { kind = "Income", accountId = s.Bank, categoryId = s.Salary, amount = 1000m, date = "2026-10-05" });

        var october = (await s.Client.GetFromJsonAsync<MovementPage>("/api/v1/transactions?from=2026-10-01&to=2026-10-31", Json))!;
        var food = (await s.Client.GetFromJsonAsync<MovementPage>($"/api/v1/transactions?categoryId={s.Food}", Json))!;
        var incomes = (await s.Client.GetFromJsonAsync<MovementPage>("/api/v1/transactions?kind=Income", Json))!;
        var search = (await s.Client.GetFromJsonAsync<MovementPage>("/api/v1/transactions?search=pedidos", Json))!;

        Assert.Equal(3, october.Total);
        Assert.Equal((1000m, 150m), (october.TotalIncome, october.TotalExpense));
        Assert.Equal(["2026-10-20", "2026-10-05", "2026-10-01"], october.Items.Select(i => i.Date.ToString("yyyy-MM-dd")));
        Assert.Equal(3, food.Total);
        Assert.Equal("Sueldo", Assert.Single(incomes.Items).Category!.Name);
        Assert.Equal(50m, Assert.Single(search.Items).Amount);
    }

    [Fact]
    public async Task List_is_paginated()
    {
        var s = await SetupAsync();
        for (var day = 1; day <= 5; day++) await Expense(s, day, $"2026-10-{day:00}");

        var page2 = (await s.Client.GetFromJsonAsync<MovementPage>("/api/v1/transactions?pageSize=2&page=2", Json))!;

        Assert.Equal(5, page2.Total);
        Assert.Equal([3m, 2m], page2.Items.Select(i => i.Amount));
    }

    [Fact]
    public async Task Expense_can_be_edited_but_not_turned_into_income()
    {
        var s = await SetupAsync();
        var expense = await Expense(s, 100m, "2026-10-16");

        var edit = await s.Client.PutAsJsonAsync($"/api/v1/transactions/{expense.Id}",
            new { kind = "Expense", accountId = s.Bank, categoryId = s.Delivery, amount = 80m, date = "2026-10-14", note = "editado" }, Json);
        var toIncome = await s.Client.PutAsJsonAsync($"/api/v1/transactions/{expense.Id}",
            new { kind = "Income", accountId = s.Bank, categoryId = s.Salary, amount = 80m, date = "2026-10-14" }, Json);

        var edited = (await edit.Content.ReadFromJsonAsync<Movement>(Json))!;
        Assert.Equal((s.Bank, "Delivery", 80m, "editado"), (edited.Account.Id, edited.Category!.Name, edited.Amount, edited.Note));
        Assert.Equal(HttpStatusCode.Conflict, toIncome.StatusCode);
        var balances = await BalancesAsync(s);
        Assert.Equal((-80m, 0m), (balances[s.Bank], balances[s.Cash]));
    }

    [Fact]
    public async Task Users_cannot_see_or_use_each_others_movements_or_accounts()
    {
        var alice = await SetupAsync();
        var bob = await SetupAsync();
        var aliceExpense = await Expense(alice, 10m, "2026-10-16");

        Assert.Equal(HttpStatusCode.NotFound, (await bob.Client.GetAsync($"/api/v1/transactions/{aliceExpense.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.Client.DeleteAsync($"/api/v1/transactions/{aliceExpense.Id}")).StatusCode);
        Assert.Empty((await bob.Client.GetFromJsonAsync<MovementPage>("/api/v1/transactions", Json))!.Items);

        var intoAliceAccount = await bob.Client.PostAsJsonAsync("/api/v1/transactions",
            new { kind = "Expense", accountId = alice.Cash, categoryId = bob.Market, amount = 1m, date = "2026-10-16" }, Json);
        var transferToAlice = await bob.Client.PostAsJsonAsync("/api/v1/transactions/transfers",
            new { fromAccountId = bob.Cash, toAccountId = alice.Cash, amount = 1m, date = "2026-10-16" }, Json);
        Assert.Equal(HttpStatusCode.NotFound, intoAliceAccount.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, transferToAlice.StatusCode);
    }
}
