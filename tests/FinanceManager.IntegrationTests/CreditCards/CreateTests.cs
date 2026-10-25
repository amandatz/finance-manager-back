using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.CreditCards.Requests;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.CreditCards;

public class CreateTests : IDisposable
{
    private const string Route = "/api/v1/credit-cards";

    private readonly CreditCardsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new CreateCreditCardRequest(1000m, 10, 20, "Visa", null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidRequest_Returns201WithLocationAndBody()
    {
        var (client, userId, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var request = new CreateCreditCardRequest(1000m, 10, 20, "Visa", null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);
        Assert.NotNull(body);
        Assert.Equal(1000m, body!.CreditLimit);
        Assert.Equal(10, body.StatementClosingDay);
        Assert.Equal(20, body.PaymentDueDay);
        Assert.Equal("Visa", body.Brand);
        Assert.Single(body.OwnerUserIds);
        Assert.Contains(userId, body.OwnerUserIds);
        Assert.True(body.IsActive);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"{Route}/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WithZeroCreditLimit_Returns400()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var request = new CreateCreditCardRequest(0m, 10, 20, null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("CreditLimit", problem!.Errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task Create_WithStatementClosingDayOutOfRange_Returns400(int day)
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var request = new CreateCreditCardRequest(1000m, day, 20, null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("StatementClosingDay", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithClosingDayEqualToDueDay_Returns400()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var request = new CreateCreditCardRequest(1000m, 10, 10, null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithBrandOver50Chars_Returns400()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var request = new CreateCreditCardRequest(1000m, 10, 20, new string('a', 51), null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Brand", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithValidSettlementAccount_Returns201()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var accountResponse = await client.PostAsJsonAsync(
            "/api/v1/financial-accounts",
            new CreateFinancialAccountRequest("Checking", FinancialAccountType.Bank, null));
        var account = await accountResponse.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);

        var request = new CreateCreditCardRequest(1000m, 10, 20, null, account!.Id);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);
        Assert.Equal(account.Id, body!.SettlementAccountId);
    }

    [Fact]
    public async Task Create_WithNonExistentSettlementAccount_Returns400()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var request = new CreateCreditCardRequest(1000m, 10, 20, null, Guid.NewGuid());

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithSettlementAccountFromAnotherHousehold_Returns400()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var (otherClient, _, _) = await _factory.RegisterNewHouseholdOwnerAsync("Other Household");
        var otherAccountResponse = await otherClient.PostAsJsonAsync(
            "/api/v1/financial-accounts",
            new CreateFinancialAccountRequest("Other Checking", FinancialAccountType.Bank, null));
        var otherAccount =
            await otherAccountResponse.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);

        var request = new CreateCreditCardRequest(1000m, 10, 20, null, otherAccount!.Id);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
