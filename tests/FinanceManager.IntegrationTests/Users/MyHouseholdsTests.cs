using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Responses;

namespace FinanceManager.IntegrationTests.Users;

public class MyHouseholdsTests : IDisposable
{
    private const string Route = "/api/v1/users/me/households";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetMyHouseholds_ReturnsEveryMembership()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var secondOwnerClient = _factory.CreateClient();
        var registerResponse = await secondOwnerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new FinanceManager.Application.Users.Requests.RegisterRequest(
                "Second Owner", "second-owner-myhh@test.local", "SecondOwnerPassword123", null, "Second Household"));
        var tokens = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var authedSecondClient = _factory.CreateAuthenticatedClient(tokens!.AccessToken);
        var invite = await (await authedSecondClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        await ownerClient.PostAsJsonAsync("/api/v1/households/join", new JoinHouseholdRequest(invite!.InviteCode!));

        var response = await ownerClient.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var memberships = await response.Content.ReadFromJsonAsync<List<MembershipResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, memberships!.Count);
        Assert.Contains(memberships, m => m.HouseholdName == UserHouseholdApiFactory.SeedHouseholdName);
        Assert.Contains(memberships, m => m.HouseholdName == "Second Household");
    }
}
