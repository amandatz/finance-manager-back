using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.CreditCards.Requests;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.CreditCards;

public class PaymentsTests : IDisposable
{
    private const string CreditCardsRoute = "/api/v1/credit-cards";
    private const string AccountsRoute = "/api/v1/financial-accounts";

    private readonly CreditCardsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static async Task<decimal> GetCurrentDebtAsync(HttpClient client, Guid creditCardId)
    {
        var response = await client.GetAsync($"{CreditCardsRoute}/{creditCardId}");
        var body = await response.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);
        return body!.CurrentDebt;
    }

    private static async Task<decimal> GetBalanceAsync(HttpClient client, Guid accountId)
    {
        var response = await client.GetAsync($"{AccountsRoute}/{accountId}");
        var body = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);
        return body!.Balance;
    }

    [Fact]
    public async Task CreatePayment_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new CreateCreditCardPaymentRequest(Guid.NewGuid(), 100m, new DateOnly(2026, 11, 20), null);

        var response = await client.PostAsJsonAsync($"{CreditCardsRoute}/{Guid.NewGuid()}/payments", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithValidRequest_Returns201AndCreatesTwoLinkedTransactions()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new FinanceManager.Application.Transactions.Requests.CreateTransactionRequest(
                null, creditCard.Id, category.Id, TransactionType.Expense, 500m, "BRL",
                new DateOnly(2026, 11, 16), null, null));
        var request = new CreateCreditCardPaymentRequest(
            account.Id, 500m, new DateOnly(2026, 11, 20), "Pagamento fatura");

        var response = await client.PostAsJsonAsync($"{CreditCardsRoute}/{creditCard.Id}/payments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreditCardPaymentResponse>(TestJsonOptions.Default);
        Assert.NotNull(body);
        Assert.Equal(body!.SettlementGroupId, body.AccountLeg.SettlementGroupId);
        Assert.Equal(body.SettlementGroupId, body.CreditCardLeg.SettlementGroupId);
        Assert.Equal(TransactionType.Expense, body.AccountLeg.Type);
        Assert.Equal(account.Id, body.AccountLeg.FinancialAccountId);
        Assert.Equal(TransactionType.Income, body.CreditCardLeg.Type);
        Assert.Equal(creditCard.Id, body.CreditCardLeg.CreditCardId);

        Assert.Equal(-500m, await GetBalanceAsync(client, account.Id));
        Assert.Equal(0m, await GetCurrentDebtAsync(client, creditCard.Id));
    }

    [Fact]
    public async Task CreatePayment_WithPartialAmount_ReducesDebtButLeavesRemainder()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new FinanceManager.Application.Transactions.Requests.CreateTransactionRequest(
                null, creditCard.Id, category.Id, TransactionType.Expense, 500m, "BRL",
                new DateOnly(2026, 11, 16), null, null));
        var request = new CreateCreditCardPaymentRequest(account.Id, 200m, new DateOnly(2026, 11, 20), null);

        var response = await client.PostAsJsonAsync($"{CreditCardsRoute}/{creditCard.Id}/payments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(300m, await GetCurrentDebtAsync(client, creditCard.Id));
        Assert.Equal(-200m, await GetBalanceAsync(client, account.Id));
    }

    [Fact]
    public async Task CreatePayment_PaymentLegs_AreExcludedFromGetAllByDefaultFilterButVisible()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var request = new CreateCreditCardPaymentRequest(account.Id, 100m, new DateOnly(2026, 11, 20), null);

        var paymentResponse = await client.PostAsJsonAsync($"{CreditCardsRoute}/{creditCard.Id}/payments", request);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<CreditCardPaymentResponse>(TestJsonOptions.Default);

        var listResponse = await client.GetAsync("/api/v1/transactions");
        var list = await listResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);

        Assert.Equal(2, list!.Count);
        Assert.All(list, x => Assert.Equal(payment!.SettlementGroupId, x.SettlementGroupId));
        Assert.All(list, x => Assert.Null(x.CategoryId));
    }

    [Fact]
    public async Task CreatePayment_IsAtomic_FailureLeavesNoLegsCreated()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var request = new CreateCreditCardPaymentRequest(Guid.NewGuid(), 100m, new DateOnly(2026, 11, 20), null);

        var response = await client.PostAsJsonAsync($"{CreditCardsRoute}/{creditCard.Id}/payments", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var listResponse = await client.GetAsync("/api/v1/transactions");
        var list = await listResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task CreatePayment_WithUnknownCreditCardId_Returns404()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var request = new CreateCreditCardPaymentRequest(account.Id, 100m, new DateOnly(2026, 11, 20), null);

        var response = await client.PostAsJsonAsync($"{CreditCardsRoute}/{Guid.NewGuid()}/payments", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithUnknownFromAccountId_Returns404()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var request = new CreateCreditCardPaymentRequest(Guid.NewGuid(), 100m, new DateOnly(2026, 11, 20), null);

        var response = await client.PostAsJsonAsync($"{CreditCardsRoute}/{creditCard.Id}/payments", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithZeroAmount_Returns400()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var request = new CreateCreditCardPaymentRequest(account.Id, 0m, new DateOnly(2026, 11, 20), null);

        var response = await client.PostAsJsonAsync($"{CreditCardsRoute}/{creditCard.Id}/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_AsNonOwnerOfCard_Returns201()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, ownerId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var (memberClient, _) = await _factory.AddHouseholdMemberAsync(ownerClient);
        var request = new CreateCreditCardPaymentRequest(account.Id, 100m, new DateOnly(2026, 11, 20), null);

        var response = await memberClient.PostAsJsonAsync($"{CreditCardsRoute}/{creditCard.Id}/payments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ArchivePayment_ArchivesBothLegsAndRestoresBalanceAndDebt()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new FinanceManager.Application.Transactions.Requests.CreateTransactionRequest(
                null, creditCard.Id, category.Id, TransactionType.Expense, 500m, "BRL",
                new DateOnly(2026, 11, 16), null, null));
        var paymentResponse = await client.PostAsJsonAsync(
            $"{CreditCardsRoute}/{creditCard.Id}/payments",
            new CreateCreditCardPaymentRequest(account.Id, 500m, new DateOnly(2026, 11, 20), null));
        var payment = await paymentResponse.Content.ReadFromJsonAsync<CreditCardPaymentResponse>(TestJsonOptions.Default);

        var archiveResponse = await client.DeleteAsync(
            $"{CreditCardsRoute}/{creditCard.Id}/payments/{payment!.SettlementGroupId}");

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
        Assert.Equal(500m, await GetCurrentDebtAsync(client, creditCard.Id));
        Assert.Equal(0m, await GetBalanceAsync(client, account.Id));

        var accountLeg = await _factory.FindTransactionAsync(payment.AccountLeg.Id);
        var creditCardLeg = await _factory.FindTransactionAsync(payment.CreditCardLeg.Id);
        Assert.False(accountLeg!.IsActive);
        Assert.False(creditCardLeg!.IsActive);
    }

    [Fact]
    public async Task ArchivePayment_WithUnknownSettlementGroupId_Returns404()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));

        var response = await client.DeleteAsync($"{CreditCardsRoute}/{creditCard.Id}/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTransaction_OnASettlementLegDirectly_Returns400AndDoesNotArchiveEitherLeg()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var paymentResponse = await client.PostAsJsonAsync(
            $"{CreditCardsRoute}/{creditCard.Id}/payments",
            new CreateCreditCardPaymentRequest(account.Id, 100m, new DateOnly(2026, 11, 20), null));
        var payment = await paymentResponse.Content.ReadFromJsonAsync<CreditCardPaymentResponse>(TestJsonOptions.Default);

        var response = await client.DeleteAsync($"/api/v1/transactions/{payment!.AccountLeg.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var accountLeg = await _factory.FindTransactionAsync(payment.AccountLeg.Id);
        var creditCardLeg = await _factory.FindTransactionAsync(payment.CreditCardLeg.Id);
        Assert.True(accountLeg!.IsActive);
        Assert.True(creditCardLeg!.IsActive);
    }
}
