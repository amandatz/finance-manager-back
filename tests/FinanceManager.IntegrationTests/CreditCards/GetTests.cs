using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.CreditCards;

public class GetTests : IDisposable
{
    private const string Route = "/api/v1/credit-cards";

    private readonly CreditCardsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetById_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithActiveCard_Returns200()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20, "Visa"));

        var response = await client.GetAsync($"{Route}/{card.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);
        Assert.Equal(card.Id, body!.Id);
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJsonOptions.Default);
        Assert.NotNull(problem);
    }

    [Fact]
    public async Task GetById_WithDeactivatedCard_Returns404()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = new CreditCard(householdId, userId, 1000m, 10, 20);
        card.Deactivate();
        await _factory.SeedCreditCardAsync(card);

        var response = await client.GetAsync($"{Route}/{card.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithActiveCards_ReturnsThem()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 2000m, 5, 15));

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CreditCardResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetAll_WithNoCards_ReturnsEmptyArray()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CreditCardResponse>>(TestJsonOptions.Default);
        Assert.Empty(body!);
    }
}
