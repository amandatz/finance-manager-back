using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Users.Requests;

namespace FinanceManager.IntegrationTests.Users;

public class ChangePasswordTests : IDisposable
{
    private const string Route = "/api/v1/users/me/password";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Returns204AndUpdates()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsOwnerAsync(client);
        var authedClient = _factory.CreateAuthenticatedClient(token);
        const string newPassword = "NewPassword456";

        var response = await authedClient.PostAsJsonAsync(
            Route,
            new ChangePasswordRequest(UserHouseholdApiFactory.SeedOwnerPassword, newPassword));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var loginClient = _factory.CreateClient();

        var newPasswordLogin = await loginClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, newPassword));
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);

        var oldPasswordLogin = await loginClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, UserHouseholdApiFactory.SeedOwnerPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsOwnerAsync(client);
        var authedClient = _factory.CreateAuthenticatedClient(token);

        var response = await authedClient.PostAsJsonAsync(
            Route,
            new ChangePasswordRequest("WrongCurrentPassword123", "NewPassword456"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
