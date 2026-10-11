using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class UpdateTests : IDisposable
{
    private const string Route = "/api/v1/financial-accounts";

    private readonly FinancialAccountsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Update_WithValidRequest_Returns200WithUpdatedAtSet()
    {
        var account = await _factory.SeedAccountAsync(new FinancialAccount("Checking", FinancialAccountType.Bank));
        var client = _factory.CreateClient();
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, "New Bank");

        var response = await client.PutAsJsonAsync($"{Route}/{account.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);
        Assert.Equal("Savings", body!.Name);
        Assert.NotNull(body.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithEmptyName_Returns400()
    {
        var account = await _factory.SeedAccountAsync(new FinancialAccount("Checking", FinancialAccountType.Bank));
        var client = _factory.CreateClient();
        var request = new UpdateFinancialAccountRequest(string.Empty, FinancialAccountType.Bank, null);

        var response = await client.PutAsJsonAsync($"{Route}/{account.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, null);

        var response = await client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDeactivatedAccount_Returns404()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank);
        account.Deactivate();
        await _factory.SeedAccountAsync(account);

        var client = _factory.CreateClient();
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, null);

        var response = await client.PutAsJsonAsync($"{Route}/{account.Id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
