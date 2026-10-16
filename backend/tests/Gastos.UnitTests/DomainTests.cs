using Gastos.Domain.Budgets;
using Gastos.Domain.Categories;
using Gastos.Domain.Common;
using Gastos.Domain.Transactions;

namespace Gastos.UnitTests;

public class DomainTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public void Transfer_creates_matching_out_and_in_rows()
    {
        var (outRow, inRow) = Transaction.Transfer(UserId, Guid.NewGuid(), Guid.NewGuid(), 1500m, Today);

        Assert.Equal(-1500m, outRow.SignedAmount);
        Assert.Equal(1500m, inRow.SignedAmount);
        Assert.Equal(outRow.TransferId, inRow.TransferId);
    }

    [Fact]
    public void Transfer_to_same_account_is_rejected()
    {
        var account = Guid.NewGuid();
        Assert.Throws<DomainException>(() => Transaction.Transfer(UserId, account, account, 10m, Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Expense_requires_positive_amount(decimal amount) =>
        Assert.Throws<DomainException>(() => Transaction.Expense(UserId, Guid.NewGuid(), Guid.NewGuid(), amount, Today));

    [Fact]
    public void Expense_reduces_balance() =>
        Assert.Equal(-250.13m, Transaction.Expense(UserId, Guid.NewGuid(), Guid.NewGuid(), 250.129m, Today).SignedAmount);

    [Fact]
    public void Budget_month_is_normalized_to_first_day() =>
        Assert.Equal(new DateOnly(2026, 10, 1), new Budget(UserId, Today, Guid.NewGuid(), 100m).Month);

    [Fact]
    public void Categories_allow_only_two_levels()
    {
        var food = new Category(UserId, "Comida", CategoryType.Expense);
        var market = new Category(UserId, "Supermercado", CategoryType.Expense, food);

        Assert.Equal(food.Id, market.ParentId);
        Assert.Throws<DomainException>(() => new Category(UserId, "Verdulería", CategoryType.Expense, market));
    }

    [Fact]
    public void Subcategory_must_match_parent_type()
    {
        var salary = new Category(UserId, "Sueldo", CategoryType.Income);
        Assert.Throws<DomainException>(() => new Category(UserId, "Bono", CategoryType.Expense, salary));
    }
}
