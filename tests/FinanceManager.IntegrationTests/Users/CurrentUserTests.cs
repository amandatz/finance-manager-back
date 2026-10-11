using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Users;

public class CurrentUserTests : IDisposable
{
    private const string Route = "/api/v1/users/me";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetMe_WithValidToken_Returns200WithProfile()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsOwnerAsync(client);
        var authedClient = _factory.CreateAuthenticatedClient(token);

        var response = await authedClient.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);
        Assert.Equal(UserHouseholdApiFactory.SeedOwnerEmail, body!.Email);
        Assert.Equal(UserHouseholdApiFactory.SeedOwnerName, body.Name);
        Assert.Equal(HouseholdRole.Owner, body.Role);
    }

    [Fact]
    public async Task GetMe_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateAuthenticatedClient("not-a-real-token");

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
