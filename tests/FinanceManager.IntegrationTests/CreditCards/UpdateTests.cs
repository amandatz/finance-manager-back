using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.CreditCards.Requests;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Domain.Entities;

namespace FinanceManager.IntegrationTests.CreditCards;

public class UpdateTests : IDisposable
{
    private const string Route = "/api/v1/credit-cards";

    private readonly CreditCardsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Update_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new UpdateCreditCardRequest(1000m, 10, 20, null, null);

        var response = await client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidRequest_Returns200WithUpdatedAtSet()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20, "Visa"));
        var request = new UpdateCreditCardRequest(2000m, 5, 15, "Mastercard", null);

        var response = await client.PutAsJsonAsync($"{Route}/{card.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreditCardResponse>(TestJsonOptions.Default);
        Assert.Equal(2000m, body!.CreditLimit);
        Assert.Equal("Mastercard", body.Brand);
        Assert.NotNull(body.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithClosingDayEqualToDueDay_Returns400()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));
        var request = new UpdateCreditCardRequest(1000m, 15, 15, null, null);

        var response = await client.PutAsJsonAsync($"{Route}/{card.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();
        var request = new UpdateCreditCardRequest(1000m, 10, 20, null, null);

        var response = await client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsNonOwnerSameHousehold_Returns404()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = await _factory.SeedCreditCardAsync(new CreditCard(householdId, ownerId, 1000m, 10, 20));
        var (memberClient, _) = await _factory.AddHouseholdMemberAsync(ownerClient);
        var request = new UpdateCreditCardRequest(2000m, 10, 20, null, null);

        var response = await memberClient.PutAsJsonAsync($"{Route}/{card.Id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsNonOwnerSameHousehold_Returns200()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = await _factory.SeedCreditCardAsync(new CreditCard(householdId, ownerId, 1000m, 10, 20));
        var (memberClient, _) = await _factory.AddHouseholdMemberAsync(ownerClient);

        var response = await memberClient.GetAsync($"{Route}/{card.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
