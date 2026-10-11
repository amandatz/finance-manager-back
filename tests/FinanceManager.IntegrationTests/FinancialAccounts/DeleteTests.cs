using System.Net;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class DeleteTests : IDisposable
{
    private const string Route = "/api/v1/financial-accounts";

    private readonly FinancialAccountsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Delete_WithActiveAccount_Returns204AndDeactivates()
    {
        var account = await _factory.SeedAccountAsync(new FinancialAccount("Checking", FinancialAccountType.Bank));
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var stored = await _factory.FindAccountAsync(account.Id);
        Assert.NotNull(stored);
        Assert.False(stored!.IsActive);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task Delete_WithUnknownId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAlreadyDeactivatedAccount_Returns404()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank);
        account.Deactivate();
        await _factory.SeedAccountAsync(account);

        var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
