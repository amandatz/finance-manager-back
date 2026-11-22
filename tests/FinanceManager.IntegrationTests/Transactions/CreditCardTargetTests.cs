using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Application.Transactions.Requests;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Transactions;

public class CreditCardTargetTests : IDisposable
{
    private const string TransactionsRoute = "/api/v1/transactions";
    private const string CreditCardsRoute = "/api/v1/credit-cards";

    private readonly LedgerApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, Guid UserId, Guid HouseholdId)> CreateAuthenticatedOwnerAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var userId = Guid.Parse(jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        var householdId = Guid.Parse(jwt.Claims.First(c => c.Type == "household_id").Value);

        return (_factory.CreateAuthenticatedClient(tokens.AccessToken), userId, householdId);
    }

    private static async Task<decimal> GetCurrentDebtAsync(HttpClient client, Guid creditCardId)
    {
        var response = await client.GetAsync($"{CreditCardsRoute}/{creditCardId}");
        var body = await response.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);
        return body!.CurrentDebt;
    }

    [Fact]
    public async Task Create_WithCreditCardId_Returns201AndDoesNotAffectAnyAccountBalance()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20, "Visa"));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            null, creditCard.Id, category.Id, TransactionType.Expense, 150m, "BRL",
            new DateOnly(2026, 11, 16), "Mercado", null);

        var response = await client.PostAsJsonAsync(TransactionsRoute, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);
        Assert.Equal(creditCard.Id, body!.CreditCardId);
        Assert.Null(body.FinancialAccountId);

        var accountResponse = await client.GetAsync($"/api/v1/financial-accounts/{account.Id}");
        var accountBody =
            await accountResponse.Content.ReadFromJsonAsync<FinanceManager.Application.FinancialAccounts.Responses.FinancialAccountResponse>(
                TestJsonOptions.Default);
        Assert.Equal(0m, accountBody!.Balance);
    }

    [Fact]
    public async Task Create_WithBothFinancialAccountIdAndCreditCardId_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, creditCard.Id, category.Id, TransactionType.Expense, 50m, "BRL",
            new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(TransactionsRoute, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNeitherFinancialAccountIdNorCreditCardId_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            null, null, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(TransactionsRoute, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownCreditCardId_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            null, Guid.NewGuid(), category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(TransactionsRoute, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDeactivatedCreditCard_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = new CreditCard(householdId, userId, 1000m, 10, 20);
        creditCard.Deactivate();
        await _factory.SeedCreditCardAsync(creditCard);
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            null, creditCard.Id, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(TransactionsRoute, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithCreditCardFromAnotherHousehold_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();
        var (_, otherOwnerId, otherHouseholdId) = await _factory.RegisterNewHouseholdOwnerAsync("Other Household");
        var otherCard = await _factory.SeedCreditCardAsync(
            new CreditCard(otherHouseholdId, otherOwnerId, 1000m, 10, 20));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, otherHouseholdId));

        var request = new CreateTransactionRequest(
            null, otherCard.Id, Guid.NewGuid(), TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(TransactionsRoute, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Expense_AgainstCreditCard_IncreasesCurrentDebt()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            null, creditCard.Id, category.Id, TransactionType.Expense, 150m, "BRL", new DateOnly(2026, 11, 16), null, null);

        await client.PostAsJsonAsync(TransactionsRoute, request);

        Assert.Equal(150m, await GetCurrentDebtAsync(client, creditCard.Id));
    }

    [Fact]
    public async Task Income_AgainstCreditCard_DecreasesCurrentDebt()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var expenseCategory = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Estorno", CategoryType.Income, householdId));

        await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                null, creditCard.Id, expenseCategory.Id, TransactionType.Expense, 150m, "BRL",
                new DateOnly(2026, 11, 16), null, null));
        await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                null, creditCard.Id, incomeCategory.Id, TransactionType.Income, 50m, "BRL",
                new DateOnly(2026, 11, 17), null, null));

        Assert.Equal(100m, await GetCurrentDebtAsync(client, creditCard.Id));
    }

    [Fact]
    public async Task EditingExpenseAmount_RecalculatesCurrentDebt()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var createResponse = await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                null, creditCard.Id, category.Id, TransactionType.Expense, 150m, "BRL",
                new DateOnly(2026, 11, 16), null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);

        await client.PutAsJsonAsync(
            $"{TransactionsRoute}/{created!.Id}",
            new UpdateTransactionRequest(
                category.Id, TransactionType.Expense, 200m, "BRL", new DateOnly(2026, 11, 16), null, null));

        Assert.Equal(200m, await GetCurrentDebtAsync(client, creditCard.Id));
    }

    [Fact]
    public async Task ArchivingExpense_RemovesItFromCurrentDebt()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var createResponse = await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                null, creditCard.Id, category.Id, TransactionType.Expense, 150m, "BRL",
                new DateOnly(2026, 11, 16), null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);

        await client.DeleteAsync($"{TransactionsRoute}/{created!.Id}");

        Assert.Equal(0m, await GetCurrentDebtAsync(client, creditCard.Id));
    }

    [Fact]
    public async Task NewCreditCard_WithNoTransactions_HasZeroCurrentDebt()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));

        Assert.Equal(0m, await GetCurrentDebtAsync(client, creditCard.Id));
    }

    [Fact]
    public async Task Update_CannotMoveTransactionFromCreditCardToAccount()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var transaction = await _factory.SeedTransactionAsync(Transaction.ForCreditCard(
            creditCard.Id, category.Id, userId, TransactionType.Expense, 150m, "BRL",
            new DateOnly(2026, 11, 16), householdId));
        var request = new UpdateTransactionRequest(
            category.Id, TransactionType.Expense, 150m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PutAsJsonAsync($"{TransactionsRoute}/{transaction.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);
        Assert.Equal(creditCard.Id, body!.CreditCardId);
        Assert.Null(body.FinancialAccountId);
    }

    [Fact]
    public async Task GetAll_FilteredByCreditCardId_ReturnsOnlyThatCardsTransactions()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var creditCard = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        var match = await _factory.SeedTransactionAsync(Transaction.ForCreditCard(
            creditCard.Id, category.Id, userId, TransactionType.Expense, 150m, "BRL",
            new DateOnly(2026, 11, 16), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 50m, "BRL",
            new DateOnly(2026, 11, 16), householdId));

        var response = await client.GetAsync($"{TransactionsRoute}?creditCardId={creditCard.Id}");

        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        var transaction = Assert.Single(body!);
        Assert.Equal(match.Id, transaction.Id);
    }
}
