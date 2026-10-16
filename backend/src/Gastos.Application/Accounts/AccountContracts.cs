using Gastos.Domain.Accounts;

namespace Gastos.Application.Accounts;

public sealed record AccountDto(Guid Id, string Name, AccountType Type, string Currency, decimal InitialBalance, decimal Balance, bool IsArchived);

public sealed record SaveAccountRequest(string Name, AccountType Type, decimal InitialBalance, bool IsArchived = false);
