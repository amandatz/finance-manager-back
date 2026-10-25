using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class ListTests : IDisposable
{
    private const string Route = "/api/v1/financial-accounts";

    private readonly FinancialAccountsApiFactory _factory = new();

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

    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithActiveAccounts_ReturnsOrderedByName()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        await _factory.SeedAccountAsync(new FinancialAccount("Zeta", FinancialAccountType.Bank, householdId, userId));
        await _factory.SeedAccountAsync(new FinancialAccount("Alpha", FinancialAccountType.Bank, householdId, userId));

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<FinancialAccountResponse>>(TestJsonOptions.Default);
        Assert.Equal(["Alpha", "Zeta"], body!.Select(x => x.Name));
    }

    [Fact]
    public async Task GetAll_WithDeactivatedAccount_ExcludesIt()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var active = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));

        var deactivated = new FinancialAccount("Old Savings", FinancialAccountType.Bank, householdId, userId);
        deactivated.Deactivate();
        await _factory.SeedAccountAsync(deactivated);

        var response = await client.GetAsync(Route);

        var body = await response.Content.ReadFromJsonAsync<List<FinancialAccountResponse>>(TestJsonOptions.Default);
        var account = Assert.Single(body!);
        Assert.Equal(active.Id, account.Id);
    }

    [Fact]
    public async Task GetAll_WithNoActiveAccounts_ReturnsEmptyArray()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<FinancialAccountResponse>>(TestJsonOptions.Default);
        Assert.Empty(body!);
    }
}
