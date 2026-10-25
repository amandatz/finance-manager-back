using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.CreditCards.Requests;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests;

/// <summary>
/// The main success criterion for the multi-tenant isolation work: a user in household A can
/// never list, get-by-id, update, or delete a FinancialAccount or CreditCard belonging to
/// household B, even knowing the exact id - direct access returns 404, indistinguishable from
/// "doesn't exist", via the EF Core global query filter on every <c>IHouseholdScoped</c>
/// entity (see <c>FinanceManagerDbContext.ApplyHouseholdScopedQueryFilters</c>).
///
/// Two independent households are created via <c>RegisterNewHouseholdOwnerAsync</c> (register
/// with no invite code - an invite code would join the SAME household, which is the opposite
/// of what this suite needs), never via <c>UserHouseholdApiFactory</c>'s default seed owner
/// for both sides, so household A and household B are genuinely different households with
/// genuinely different owners.
/// </summary>
public class CrossHouseholdIsolationTests : IDisposable
{
    private const string AccountsRoute = "/api/v1/financial-accounts";
    private const string CreditCardsRoute = "/api/v1/credit-cards";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // --- FinancialAccount ---

    [Fact]
    public async Task List_NeverReturnsAnotherHouseholdsAccounts()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");

        await householdBClient.PostAsJsonAsync(
            AccountsRoute, new CreateFinancialAccountRequest("Household B Checking", FinancialAccountType.Bank, null));

        var response = await householdAClient.GetAsync(AccountsRoute);
        var body = await response.Content.ReadFromJsonAsync<List<FinancialAccountResponse>>(TestJsonOptions.Default);

        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetById_ForAnotherHouseholdsAccount_Returns404()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var createResponse = await householdBClient.PostAsJsonAsync(
            AccountsRoute, new CreateFinancialAccountRequest("Household B Checking", FinancialAccountType.Bank, null));
        var account = await createResponse.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);

        var response = await householdAClient.GetAsync($"{AccountsRoute}/{account!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherHouseholdsAccount_Returns404()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var createResponse = await householdBClient.PostAsJsonAsync(
            AccountsRoute, new CreateFinancialAccountRequest("Household B Checking", FinancialAccountType.Bank, null));
        var account = await createResponse.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);

        var response = await householdAClient.PutAsJsonAsync(
            $"{AccountsRoute}/{account!.Id}",
            new UpdateFinancialAccountRequest("Hijacked", FinancialAccountType.Bank, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ForAnotherHouseholdsAccount_Returns404AndDoesNotDeactivate()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var createResponse = await householdBClient.PostAsJsonAsync(
            AccountsRoute, new CreateFinancialAccountRequest("Household B Checking", FinancialAccountType.Bank, null));
        var account = await createResponse.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);

        var response = await householdAClient.DeleteAsync($"{AccountsRoute}/{account!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Confirm household B still sees its own account, untouched, proving the attempted
        // cross-household delete from household A had no effect.
        var getResponse = await householdBClient.GetAsync($"{AccountsRoute}/{account.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task FinancialAccount_Create_RoundTripsOwnerCorrectly()
    {
        var (client, userId, _) = await _factory.CreateAuthenticatedOwnerAsync();

        var response = await client.PostAsJsonAsync(
            AccountsRoute, new CreateFinancialAccountRequest("Checking", FinancialAccountType.Bank, null));
        var created = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);

        var getResponse = await client.GetAsync($"{AccountsRoute}/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);

        Assert.Single(fetched!.OwnerUserIds);
        Assert.Contains(userId, fetched.OwnerUserIds);
    }

    // --- CreditCard ---

    [Fact]
    public async Task List_NeverReturnsAnotherHouseholdsCreditCards()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");

        await householdBClient.PostAsJsonAsync(CreditCardsRoute, new CreateCreditCardRequest(1000m, 10, 20, null, null));

        var response = await householdAClient.GetAsync(CreditCardsRoute);
        var body = await response.Content.ReadFromJsonAsync<List<CreditCardResponse>>(TestJsonOptions.Default);

        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetById_ForAnotherHouseholdsCreditCard_Returns404()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var createResponse = await householdBClient.PostAsJsonAsync(
            CreditCardsRoute, new CreateCreditCardRequest(1000m, 10, 20, null, null));
        var card = await createResponse.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);

        var response = await householdAClient.GetAsync($"{CreditCardsRoute}/{card!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherHouseholdsCreditCard_Returns404()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var createResponse = await householdBClient.PostAsJsonAsync(
            CreditCardsRoute, new CreateCreditCardRequest(1000m, 10, 20, null, null));
        var card = await createResponse.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);

        var response = await householdAClient.PutAsJsonAsync(
            $"{CreditCardsRoute}/{card!.Id}",
            new UpdateCreditCardRequest(5000m, 10, 20, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ForAnotherHouseholdsCreditCard_Returns404AndDoesNotDeactivate()
    {
        var (householdAClient, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (householdBClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var createResponse = await householdBClient.PostAsJsonAsync(
            CreditCardsRoute, new CreateCreditCardRequest(1000m, 10, 20, null, null));
        var card = await createResponse.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);

        var response = await householdAClient.DeleteAsync($"{CreditCardsRoute}/{card!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var getResponse = await householdBClient.GetAsync($"{CreditCardsRoute}/{card.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreditCard_Create_RoundTripsOwnerCorrectly()
    {
        var (client, userId, _) = await _factory.CreateAuthenticatedOwnerAsync();

        var response = await client.PostAsJsonAsync(
            CreditCardsRoute, new CreateCreditCardRequest(1000m, 10, 20, "Visa", null));
        var created = await response.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);

        var getResponse = await client.GetAsync($"{CreditCardsRoute}/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);

        Assert.Single(fetched!.OwnerUserIds);
        Assert.Contains(userId, fetched.OwnerUserIds);
    }
}
