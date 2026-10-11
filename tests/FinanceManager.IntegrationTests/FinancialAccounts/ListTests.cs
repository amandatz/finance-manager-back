using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class ListTests : IDisposable
{
    private const string Route = "/api/v1/financial-accounts";

    private readonly FinancialAccountsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetAll_WithActiveAccounts_ReturnsOrderedByName()
    {
        await _factory.SeedAccountAsync(new FinancialAccount("Zeta", FinancialAccountType.Bank));
        await _factory.SeedAccountAsync(new FinancialAccount("Alpha", FinancialAccountType.Bank));

        var client = _factory.CreateClient();
        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<FinancialAccountResponse>>(TestJsonOptions.Default);
        Assert.Equal(["Alpha", "Zeta"], body!.Select(x => x.Name));
    }

    [Fact]
    public async Task GetAll_WithDeactivatedAccount_ExcludesIt()
    {
        var active = await _factory.SeedAccountAsync(new FinancialAccount("Checking", FinancialAccountType.Bank));

        var deactivated = new FinancialAccount("Old Savings", FinancialAccountType.Bank);
        deactivated.Deactivate();
        await _factory.SeedAccountAsync(deactivated);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(Route);

        var body = await response.Content.ReadFromJsonAsync<List<FinancialAccountResponse>>(TestJsonOptions.Default);
        var account = Assert.Single(body!);
        Assert.Equal(active.Id, account.Id);
    }

    [Fact]
    public async Task GetAll_WithNoActiveAccounts_ReturnsEmptyArray()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<FinancialAccountResponse>>(TestJsonOptions.Default);
        Assert.Empty(body!);
    }
}
