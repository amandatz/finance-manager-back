using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class GetByIdTests : IDisposable
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
    public async Task GetById_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithActiveAccount_Returns200()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));

        var response = await client.GetAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);
        Assert.Equal(account.Id, body!.Id);
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJsonOptions.Default);
        Assert.NotNull(problem);
    }

    [Fact]
    public async Task GetById_WithUnknownId_IncludesTraceId()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync($"{Route}/{Guid.NewGuid()}");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJsonOptions.Default);
        Assert.True(problem!.Extensions.TryGetValue("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId?.ToString()));
    }

    [Fact]
    public async Task GetById_WithDeactivatedAccount_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId);
        account.Deactivate();
        await _factory.SeedAccountAsync(account);

        var response = await client.GetAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
