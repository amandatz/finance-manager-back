using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Categories.Requests;
using FinanceManager.Application.Categories.Responses;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Categories;

public class CreateTests : IDisposable
{
    private const string Route = "/api/v1/categories";

    private readonly LedgerApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> CreateAuthenticatedOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsOwnerAsync(client);
        return _factory.CreateAuthenticatedClient(token);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new CreateCategoryRequest("Supermercado", CategoryType.Expense);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNoActiveHousehold_Returns401()
    {
        var client = await _factory.CreateClientWithNoHouseholdTokenAsync();
        var request = new CreateCategoryRequest("Supermercado", CategoryType.Expense);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidRequest_Returns201WithLocationAndBody()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateCategoryRequest("Supermercado", CategoryType.Expense);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>(TestJsonOptions.Default);
        Assert.NotNull(body);
        Assert.Equal("Supermercado", body!.Name);
        Assert.Equal(CategoryType.Expense, body.Type);
        Assert.True(body.IsActive);
        Assert.Null(body.UpdatedAt);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"{Route}/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateCategoryRequest(string.Empty, CategoryType.Expense);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithNameOver100Chars_Returns400()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateCategoryRequest(new string('a', 101), CategoryType.Expense);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithUndefinedType_Returns400()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateCategoryRequest("Supermercado", (CategoryType)999);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Type", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithDuplicateNameAndTypeInHousehold_Returns409()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        await client.PostAsJsonAsync(Route, new CreateCategoryRequest("Supermercado", CategoryType.Expense));

        var response = await client.PostAsJsonAsync(Route, new CreateCategoryRequest("supermercado", CategoryType.Expense));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithSameNameButDifferentType_Returns201()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        await client.PostAsJsonAsync(Route, new CreateCategoryRequest("Presentes", CategoryType.Income));

        var response = await client.PostAsJsonAsync(Route, new CreateCategoryRequest("Presentes", CategoryType.Expense));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsNonOwnerHouseholdMember_Returns201()
    {
        var ownerClient = await CreateAuthenticatedOwnerClientAsync();
        var memberClient = await AddHouseholdMemberAsync(ownerClient);
        var request = new CreateCategoryRequest("Pet", CategoryType.Expense);

        var response = await memberClient.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        _ = Guid.Parse(jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }
}
