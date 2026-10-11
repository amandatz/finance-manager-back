using System.Net;

namespace FinanceManager.IntegrationTests.Users;

public class AuthFailureHandlingTests : IDisposable
{
    private readonly ThrowingUserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UnhandledException_Returns500WithBareProblemDetails()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsOwnerAsync(client);
        var authedClient = _factory.CreateAuthenticatedClient(token);

        var response = await authedClient.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("Simulated failure", body);
        Assert.DoesNotContain(" at ", body);
    }
}
