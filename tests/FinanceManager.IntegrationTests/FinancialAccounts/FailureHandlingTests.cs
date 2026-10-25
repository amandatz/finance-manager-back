using System.Net;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class FailureHandlingTests : IDisposable
{
    private readonly ThrowingFinancialAccountsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UnhandledException_Returns500WithBareProblemDetails()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsOwnerAsync(client);
        var authenticatedClient = _factory.CreateAuthenticatedClient(token);

        var response = await authenticatedClient.GetAsync("/api/v1/financial-accounts");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("Simulated failure", body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" at ", body);
    }
}
