using System.Net.Http.Json;
using FinanceManager.Application.Categories.Responses;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;

namespace FinanceManager.IntegrationTests.Categories;

public class DefaultSeedTests : IDisposable
{
    private const string Route = "/api/v1/categories";

    private readonly LedgerApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task NewHousehold_Creation_SeedsDefaultCategories()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest("New Owner", "new-owner@test.local", "TestPassword123"));
        registerResponse.EnsureSuccessStatusCode();
        var registerTokens = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);

        var pendingClient = _factory.CreateAuthenticatedClient(registerTokens!.AccessToken);
        var createHouseholdResponse = await pendingClient.PostAsJsonAsync(
            "/api/v1/households", new CreateHouseholdRequest("New Household"));
        createHouseholdResponse.EnsureSuccessStatusCode();
        var tokens = await createHouseholdResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);

        var authenticatedClient = _factory.CreateAuthenticatedClient(tokens!.AccessToken);
        var response = await authenticatedClient.GetAsync(Route);

        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default);
        Assert.NotEmpty(body!);
        Assert.Contains(body!, x => x.Name == "Salário");
        Assert.Contains(body!, x => x.Name == "Alimentação");
    }

    [Fact]
    public async Task JoiningExistingHousehold_ViaInviteCode_DoesNotSeedAdditionalCategories()
    {
        var ownerClient = _factory.CreateClient();
        await _factory.EnsureOwnerRegisteredAsync(ownerClient);
        var ownerToken = await _factory.LoginAsOwnerAsync(ownerClient);
        var authenticatedOwnerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var categoriesBeforeInvite = await authenticatedOwnerClient.GetAsync(Route);
        var countBeforeInvite = (await categoriesBeforeInvite.Content
            .ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default))!.Count;

        var inviteResponse = await authenticatedOwnerClient.PostAsync("/api/v1/households/invite-code", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        var joiningClient = _factory.CreateClient();
        var joiningRegisterResponse = await joiningClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Joining Member", "joining-member@test.local", "TestPassword123"));
        var joiningTokens = await joiningRegisterResponse.Content
            .ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var authenticatedJoiningClient = _factory.CreateAuthenticatedClient(joiningTokens!.AccessToken);
        await authenticatedJoiningClient.PostAsJsonAsync(
            "/api/v1/households/join", new JoinHouseholdRequest(invite!.InviteCode!));

        var categoriesAfterJoin = await authenticatedOwnerClient.GetAsync(Route);
        var countAfterJoin = (await categoriesAfterJoin.Content
            .ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default))!.Count;

        Assert.Equal(countBeforeInvite, countAfterJoin);
    }
}
