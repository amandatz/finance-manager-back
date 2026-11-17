using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Categories.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Categories;

public class ListTests : IDisposable
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

    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithActiveCategories_ReturnsOrderedByName()
    {
        var (client, householdId) = await CreateAuthenticatedOwnerAsync();
        await _factory.SeedCategoryAsync(new Category("Zumba", CategoryType.Expense, householdId));
        await _factory.SeedCategoryAsync(new Category("Academia", CategoryType.Expense, householdId));

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default);
        var names = body!.Select(x => x.Name).ToList();
        Assert.True(names.IndexOf("Academia") < names.IndexOf("Zumba"));
        Assert.Equal(names.OrderBy(x => x, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task GetAll_WithDeactivatedCategory_ExcludesIt()
    {
        var (client, householdId) = await CreateAuthenticatedOwnerAsync();

        var deactivated = new Category("Categoria Antiga", CategoryType.Expense, householdId);
        deactivated.Deactivate();
        await _factory.SeedCategoryAsync(deactivated);

        var response = await client.GetAsync(Route);

        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default);
        Assert.DoesNotContain(body!, x => x.Id == deactivated.Id);
    }

    [Fact]
    public async Task GetAll_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        var (client, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync($"{Route}?type={(int)CategoryType.Income}");

        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default);
        Assert.NotEmpty(body!);
        Assert.All(body!, x => Assert.Equal(CategoryType.Income, x.Type));
    }

    [Fact]
    public async Task GetAll_ForNewHousehold_ReturnsDefaultSeedCategories()
    {
        var (client, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default);
        Assert.NotEmpty(body!);
    }
}
