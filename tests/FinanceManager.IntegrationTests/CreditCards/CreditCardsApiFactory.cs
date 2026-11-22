namespace FinanceManager.IntegrationTests.CreditCards;

/// <summary>
/// CreditCard tests need accounts, categories, and transactions too (e.g. bill payments link a
/// CreditCard to a FinancialAccount via settlement transactions), so this reuses every seed
/// helper <see cref="LedgerApiFactory"/> already has rather than duplicating them.
/// </summary>
public class CreditCardsApiFactory : LedgerApiFactory;
