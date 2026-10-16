using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gastos.IntegrationTests;

public sealed class BudgetsAndDashboardTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private sealed record Account(Guid Id, string Name, decimal Balance);
    private sealed record Category(Guid Id, string Name, List<Category> Children);
    private sealed record BudgetLine(Guid CategoryId, string Name, decimal Budgeted, decimal Spent);
    private sealed record BudgetMonth(DateOnly Month, List<BudgetLine> Lines, decimal TotalBudgeted, decimal TotalSpent);
    private sealed record CategorySpending(Guid CategoryId, string Name, decimal Spent, decimal Budgeted);
    private sealed record MonthTotals(DateOnly Month, decimal Income, decimal Expense);
    private sealed record Dashboard(DateOnly Month, decimal Income, decimal Expense, decimal Budgeted, decimal TotalBalance, List<CategorySpending> ByCategory, List<MonthTotals> LastMonths);

    private sealed record Setup(HttpClient Client, Guid Cash, Guid Food, Guid Market, Guid Transport, Guid Salary, Guid Bank);

    private async Task<Setup> SetupAsync()
    {
        var (client, _) = await factory.CreateUserAsync();
        var bank = await (await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Banco", type = "Bank", initialBalance = 1000m }, Json))
            .Content.ReadFromJsonAsync<Account>(Json);
        var cash = (await client.GetFromJsonAsync<List<Account>>("/api/v1/accounts", Json))!.Single(a => a.Name == "Efectivo");
        var expenses = (await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Expense", Json))!;
        var incomes = (await client.GetFromJsonAsync<List<Category>>("/api/v1/categories?type=Income", Json))!;
        var food = expenses.Single(c => c.Name == "Comida");
        return new Setup(client, cash.Id, food.Id, food.Children.Single(c => c.Name == "Supermercado").Id,
            expenses.Single(c => c.Name == "Transporte").Id, incomes.Single(c => c.Name == "Sueldo").Id, bank!.Id);
    }

    private static async Task MoveAsync(Setup s, string kind, Guid category, decimal amount, string date)
    {
        var response = await s.Client.PostAsJsonAsync("/api/v1/transactions", new { kind, accountId = s.Cash, categoryId = category, amount, date }, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task SetBudgetAsync(Setup s, string month, Guid category, decimal amount)
    {
        var response = await s.Client.PutAsJsonAsync($"/api/v1/budgets/{month}/{category}", new { amount }, Json);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Budget_lines_compare_planned_against_spent_including_subcategories()
    {
        var s = await SetupAsync();
        await SetBudgetAsync(s, "2026-10-15", s.Food, 100_000m);
        await MoveAsync(s, "Expense", s.Market, 30_000m, "2026-10-02");
        await MoveAsync(s, "Expense", s.Food, 5_000m, "2026-10-20");
        await MoveAsync(s, "Expense", s.Transport, 2_000m, "2026-10-21");
        await MoveAsync(s, "Expense", s.Market, 9_999m, "2026-11-01");

        var month = (await s.Client.GetFromJsonAsync<BudgetMonth>("/api/v1/budgets/2026-10-01", Json))!;

        Assert.Equal(new DateOnly(2026, 10, 1), month.Month);
        var food = month.Lines[0];
        Assert.Equal((s.Food, 100_000m, 35_000m), (food.CategoryId, food.Budgeted, food.Spent));
        Assert.Equal((0m, 2_000m), (month.Lines.Single(l => l.CategoryId == s.Transport).Budgeted, month.Lines.Single(l => l.CategoryId == s.Transport).Spent));
        Assert.DoesNotContain(month.Lines, l => l.CategoryId == s.Market);
        Assert.Equal((100_000m, 37_000m), (month.TotalBudgeted, month.TotalSpent));
    }

    [Fact]
    public async Task Setting_zero_removes_the_budget_and_only_top_level_expense_categories_are_allowed()
    {
        var s = await SetupAsync();
        await SetBudgetAsync(s, "2026-10-01", s.Food, 50m);
        await SetBudgetAsync(s, "2026-10-01", s.Food, 0m);

        var month = (await s.Client.GetFromJsonAsync<BudgetMonth>("/api/v1/budgets/2026-10-01", Json))!;
        Assert.Equal(0m, month.TotalBudgeted);

        Assert.Equal(HttpStatusCode.NotFound, (await s.Client.PutAsJsonAsync($"/api/v1/budgets/2026-10-01/{s.Market}", new { amount = 1m }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await s.Client.PutAsJsonAsync($"/api/v1/budgets/2026-10-01/{s.Salary}", new { amount = 1m }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Client.PutAsJsonAsync($"/api/v1/budgets/2026-10-01/{s.Food}", new { amount = -1m }, Json)).StatusCode);
    }

    [Fact]
    public async Task Copy_previous_fills_only_categories_without_a_budget()
    {
        var s = await SetupAsync();
        await SetBudgetAsync(s, "2026-09-01", s.Food, 80_000m);
        await SetBudgetAsync(s, "2026-09-01", s.Transport, 20_000m);
        await SetBudgetAsync(s, "2026-10-01", s.Transport, 25_000m);

        var response = await s.Client.PostAsync("/api/v1/budgets/2026-10-01/copy-previous", null);
        var month = (await response.Content.ReadFromJsonAsync<BudgetMonth>(Json))!;

        Assert.Equal(80_000m, month.Lines.Single(l => l.CategoryId == s.Food).Budgeted);
        Assert.Equal(25_000m, month.Lines.Single(l => l.CategoryId == s.Transport).Budgeted);
        Assert.Equal(HttpStatusCode.NotFound, (await s.Client.PostAsync("/api/v1/budgets/2026-01-01/copy-previous", null)).StatusCode);
    }

    [Fact]
    public async Task Dashboard_summarises_the_month_history_and_balance()
    {
        var s = await SetupAsync();
        await SetBudgetAsync(s, "2026-10-01", s.Food, 100_000m);
        await MoveAsync(s, "Income", s.Salary, 500_000m, "2026-10-01");
        await MoveAsync(s, "Expense", s.Market, 30_000m, "2026-10-02");
        await MoveAsync(s, "Expense", s.Transport, 2_000m, "2026-10-03");
        await MoveAsync(s, "Expense", s.Transport, 7_000m, "2026-08-10");
        var transfer = await s.Client.PostAsJsonAsync("/api/v1/transactions/transfers",
            new { fromAccountId = s.Cash, toAccountId = s.Bank, amount = 10_000m, date = "2026-10-05" }, Json);
        Assert.Equal(HttpStatusCode.Created, transfer.StatusCode);

        var d = (await s.Client.GetFromJsonAsync<Dashboard>("/api/v1/dashboard?month=2026-10-16", Json))!;

        Assert.Equal((500_000m, 32_000m, 100_000m), (d.Income, d.Expense, d.Budgeted));
        Assert.Equal(1000m + 500_000m - 32_000m - 7_000m, d.TotalBalance);
        Assert.Equal(["Comida", "Transporte"], d.ByCategory.Select(c => c.Name));
        Assert.Equal((30_000m, 100_000m), (d.ByCategory[0].Spent, d.ByCategory[0].Budgeted));
        Assert.Equal(6, d.LastMonths.Count);
        Assert.Equal(new DateOnly(2026, 5, 1), d.LastMonths[0].Month);
        Assert.Equal(7_000m, d.LastMonths.Single(m => m.Month == new DateOnly(2026, 8, 1)).Expense);
    }

    [Fact]
    public async Task Budgets_are_private_to_each_user()
    {
        var s = await SetupAsync();
        await SetBudgetAsync(s, "2026-10-01", s.Food, 100m);
        var (other, _) = await factory.CreateUserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/v1/budgets/2026-10-01/{s.Food}", new { amount = 1m }, Json)).StatusCode);
        var month = (await other.GetFromJsonAsync<BudgetMonth>("/api/v1/budgets/2026-10-01", Json))!;
        Assert.Equal(0m, month.TotalBudgeted);
    }
}
