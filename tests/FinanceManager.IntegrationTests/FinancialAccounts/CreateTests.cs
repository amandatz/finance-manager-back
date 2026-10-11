using System.Globalization;
using System.Net;
using System.Net.Http.Json;
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

    [Fact]
    public async Task Create_WithValidRequest_Returns201WithLocationAndBody()
    {
        var client = _factory.CreateClient();
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
    public async Task Create_WithEmptyName_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new CreateFinancialAccountRequest(string.Empty, FinancialAccountType.Bank, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithNameOver100Chars_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new CreateFinancialAccountRequest(new string('a', 101), FinancialAccountType.Bank, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithInstitutionNameOver150Chars_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new CreateFinancialAccountRequest("Checking", FinancialAccountType.Bank, new string('a', 151));

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("InstitutionName", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithUndefinedType_Returns400()
    {
        var client = _factory.CreateClient();
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
            var client = _factory.CreateClient();
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
