using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Categories.Requests;
using FinanceManager.Application.Categories.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Categories;

public class UpdateAndDeleteTests : IDisposable
{
    private const string Route = "/api/v1/categories";

    private readonly LedgerApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, Guid HouseholdId)> CreateAuthenticatedOwnerAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var householdId = Guid.Parse(jwt.Claims.First(c => c.Type == "household_id").Value);

        return (_factory.CreateAuthenticatedClient(tokens.AccessToken), householdId);
    }

    private async Task<HttpClient> AddHouseholdMemberAsync(HttpClient ownerClient)
    {
        var email = $"member-{Guid.NewGuid()}@test.local";
        const string password = "MemberPassword123";

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/users",
            new FinanceManager.Application.Users.Requests.CreateMemberRequest("Household Member", email, password));
        createResponse.EnsureSuccessStatusCode();

        var loginClient = _factory.CreateClient();
        var tokens = await _factory.LoginWithTokensAsync(loginClient, email, password);

        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    [Fact]
    public async Task Update_WithValidRequest_Returns200WithUpdatedAtSet()
    {
        var (client, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Supermercado", CategoryType.Expense, householdId));
        var request = new UpdateCategoryRequest("Mercado", CategoryType.Expense);

        var response = await client.PutAsJsonAsync($"{Route}/{category.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>(TestJsonOptions.Default);
        Assert.Equal("Mercado", body!.Name);
        Assert.NotNull(body.UpdatedAt);
    }

    [Fact]
    public async Task Update_AsNonOwnerHouseholdMember_Returns200()
    {
        var (ownerClient, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Supermercado", CategoryType.Expense, householdId));
        var memberClient = await AddHouseholdMemberAsync(ownerClient);
        var request = new UpdateCategoryRequest("Mercado", CategoryType.Expense);

        var response = await memberClient.PutAsJsonAsync($"{Route}/{category.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404()
    {
        var (client, _) = await CreateAuthenticatedOwnerAsync();
        var request = new UpdateCategoryRequest("Mercado", CategoryType.Expense);

        var response = await client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDuplicateNameAndTypeOfAnotherCategory_Returns409()
    {
        var (client, householdId) = await CreateAuthenticatedOwnerAsync();
        await _factory.SeedCategoryAsync(new Category("Mercado", CategoryType.Expense, householdId));
        var category = await _factory.SeedCategoryAsync(new Category("Supermercado", CategoryType.Expense, householdId));
        var request = new UpdateCategoryRequest("Mercado", CategoryType.Expense);

        var response = await client.PutAsJsonAsync($"{Route}/{category.Id}", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithSameNameAsItself_Returns200()
    {
        var (client, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Supermercado", CategoryType.Expense, householdId));
        var request = new UpdateCategoryRequest("Supermercado", CategoryType.Expense);

        var response = await client.PutAsJsonAsync($"{Route}/{category.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithActiveCategory_Returns204AndDeactivates()
    {
        var (client, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Supermercado", CategoryType.Expense, householdId));

        var response = await client.DeleteAsync($"{Route}/{category.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await _factory.FindCategoryAsync(category.Id);
        Assert.False(stored!.IsActive);
    }

    [Fact]
    public async Task Delete_AsNonOwnerHouseholdMember_Returns204()
    {
        var (ownerClient, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Supermercado", CategoryType.Expense, householdId));
        var memberClient = await AddHouseholdMemberAsync(ownerClient);

        var response = await memberClient.DeleteAsync($"{Route}/{category.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithUnknownId_Returns404()
    {
        var (client, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
