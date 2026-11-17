using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Categories.Requests;
using FinanceManager.Application.Categories.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Categories;

public class CrossHouseholdIsolationTests : IDisposable
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
    public async Task GetAll_NeverReturnsAnotherHouseholdsCategories()
    {
        var householdAClient = await CreateAuthenticatedOwnerClientAsync();
        var (_, _, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));

        var response = await householdAClient.GetAsync(Route);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(TestJsonOptions.Default);

        Assert.DoesNotContain(body!, x => x.Name == "Household B Category");
    }

    [Fact]
    public async Task GetById_ForAnotherHouseholdsCategory_Returns404()
    {
        var householdAClient = await CreateAuthenticatedOwnerClientAsync();
        var (_, _, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var categoryB = await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));

        var response = await householdAClient.GetAsync($"{Route}/{categoryB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherHouseholdsCategory_Returns404AndDoesNotMutate()
    {
        var householdAClient = await CreateAuthenticatedOwnerClientAsync();
        var (_, _, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var categoryB = await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));
        var request = new UpdateCategoryRequest("Hijacked", CategoryType.Expense);

        var response = await householdAClient.PutAsJsonAsync($"{Route}/{categoryB.Id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await _factory.FindCategoryAsync(categoryB.Id);
        Assert.Equal("Household B Category", stored!.Name);
    }

    [Fact]
    public async Task Delete_ForAnotherHouseholdsCategory_Returns404AndDoesNotDeactivate()
    {
        var householdAClient = await CreateAuthenticatedOwnerClientAsync();
        var (_, _, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var categoryB = await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));

        var response = await householdAClient.DeleteAsync($"{Route}/{categoryB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await _factory.FindCategoryAsync(categoryB.Id);
        Assert.True(stored!.IsActive);
    }
}
