using System.Net;
using FinanceManager.Domain.Entities;

namespace FinanceManager.IntegrationTests.CreditCards;

public class DeleteTests : IDisposable
{
    private const string Route = "/api/v1/credit-cards";

    private readonly CreditCardsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Delete_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithActiveCard_Returns204AndDeactivates()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = await _factory.SeedCreditCardAsync(new CreditCard(householdId, userId, 1000m, 10, 20));

        var response = await client.DeleteAsync($"{Route}/{card.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var stored = await _factory.FindCreditCardAsync(card.Id);
        Assert.NotNull(stored);
        Assert.False(stored!.IsActive);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task Delete_WithUnknownId_Returns404()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();

        var response = await client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsNonOwnerSameHousehold_Returns404AndDoesNotDeactivate()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var card = await _factory.SeedCreditCardAsync(new CreditCard(householdId, ownerId, 1000m, 10, 20));
        var (memberClient, _) = await _factory.AddHouseholdMemberAsync(ownerClient);

        var response = await memberClient.DeleteAsync($"{Route}/{card.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await _factory.FindCreditCardAsync(card.Id);
        Assert.True(stored!.IsActive);
    }
}
