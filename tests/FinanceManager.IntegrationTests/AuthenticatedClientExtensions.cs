using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;

namespace FinanceManager.IntegrationTests;

/// <summary>
/// Shared helpers for tests that need an authenticated <see cref="HttpClient"/> plus the
/// caller's user id / household id (read back out of the issued JWT) - used across
/// FinancialAccounts, CreditCards, and cross-household isolation test suites to avoid
/// repeating the same login + claim-parsing boilerplate in every file.
/// </summary>
public static class AuthenticatedClientExtensions
{
    public static async Task<(HttpClient Client, Guid UserId, Guid HouseholdId)> CreateAuthenticatedOwnerAsync(
        this UserHouseholdApiFactory factory)
    {
        var client = factory.CreateClient();
        var tokens = await factory.LoginAsOwnerWithTokensAsync(client);

        return ToClientAndClaims(factory, tokens);
    }

    /// <summary>
    /// Registers a brand-new, independent household (no invite code) and returns an
    /// authenticated client plus that owner's user id / household id - used to set up the
    /// "household B" side of cross-tenant isolation tests.
    /// </summary>
    public static async Task<(HttpClient Client, Guid UserId, Guid HouseholdId)> RegisterNewHouseholdOwnerAsync(
        this UserHouseholdApiFactory factory,
        string householdName)
    {
        var registerClient = factory.CreateClient();
        var email = $"owner-{Guid.NewGuid()}@test.local";
        const string password = "OtherHouseholdPwd123";

        var registerResponse = await registerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Other Owner", email, password, null, householdName));
        registerResponse.EnsureSuccessStatusCode();

        var tokens = (await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default))!;

        return ToClientAndClaims(factory, tokens);
    }

    /// <summary>
    /// Adds a second member (Member role) to the given owner client's current household via
    /// POST /users, then logs them in - used to prove a non-owner household member can view
    /// but not mutate an entity they don't own.
    /// </summary>
    public static async Task<(HttpClient Client, Guid UserId)> AddHouseholdMemberAsync(
        this UserHouseholdApiFactory factory,
        HttpClient ownerClient)
    {
        var email = $"member-{Guid.NewGuid()}@test.local";
        const string password = "MemberPassword123";

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/users",
            new CreateMemberRequest("Household Member", email, password));
        createResponse.EnsureSuccessStatusCode();

        var loginClient = factory.CreateClient();
        var tokens = await factory.LoginWithTokensAsync(loginClient, email, password);
        var (client, userId, _) = ToClientAndClaims(factory, tokens);

        return (client, userId);
    }

    private static (HttpClient Client, Guid UserId, Guid HouseholdId) ToClientAndClaims(
        UserHouseholdApiFactory factory,
        LoginResponse tokens)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var userId = Guid.Parse(jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        var householdId = Guid.Parse(jwt.Claims.First(c => c.Type == "household_id").Value);

        return (factory.CreateAuthenticatedClient(tokens.AccessToken), userId, householdId);
    }
}
