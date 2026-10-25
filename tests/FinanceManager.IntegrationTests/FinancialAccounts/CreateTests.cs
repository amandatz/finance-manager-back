using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class CreateTests : IDisposable
{
    private const string Route = "/api/v1/financial-accounts";

    private readonly FinancialAccountsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> CreateAuthenticatedOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsOwnerAsync(client);
        return _factory.CreateAuthenticatedClient(token);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedOwnerClientWithIdAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var userId = Guid.Parse(jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        return (_factory.CreateAuthenticatedClient(tokens.AccessToken), userId);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new CreateFinancialAccountRequest("Checking", FinancialAccountType.Bank, "Bank of Test");

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidRequest_Returns201WithLocationAndBody()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateFinancialAccountRequest("Checking", FinancialAccountType.Bank, "Bank of Test");

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);
        Assert.NotNull(body);
        Assert.True(body!.IsActive);
        Assert.NotEqual(default, body.CreatedAt);
        Assert.Null(body.UpdatedAt);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"{Route}/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WithValidRequest_SetsCallerAsSoleOwner()
    {
        var (client, callerId) = await CreateAuthenticatedOwnerClientWithIdAsync();
        var request = new CreateFinancialAccountRequest("Checking", FinancialAccountType.Bank, "Bank of Test");

        var response = await client.PostAsJsonAsync(Route, request);

        var body = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);
        Assert.Single(body!.OwnerUserIds);
        Assert.Contains(callerId, body.OwnerUserIds);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateFinancialAccountRequest(string.Empty, FinancialAccountType.Bank, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithNameOver100Chars_Returns400()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateFinancialAccountRequest(new string('a', 101), FinancialAccountType.Bank, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithInstitutionNameOver150Chars_Returns400()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateFinancialAccountRequest("Checking", FinancialAccountType.Bank, new string('a', 151));

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("InstitutionName", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithUndefinedType_Returns400()
    {
        var client = await CreateAuthenticatedOwnerClientAsync();
        var request = new CreateFinancialAccountRequest("Checking", (FinancialAccountType)999, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Type", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithEmptyNameUnderPtBrCulture_ReturnsEnglishMessage()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");

        try
        {
            var client = await CreateAuthenticatedOwnerClientAsync();
            var request = new CreateFinancialAccountRequest(string.Empty, FinancialAccountType.Bank, null);

            var response = await client.PostAsJsonAsync(Route, request);

            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
            var message = Assert.Single(problem!.Errors["Name"]);

            Assert.Contains("must not be empty", message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("vazio", message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
