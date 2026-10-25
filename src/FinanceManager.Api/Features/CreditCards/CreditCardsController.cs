using System.Security.Claims;
using FinanceManager.Application.CreditCards;
using FinanceManager.Application.CreditCards.Requests;
using FinanceManager.Application.CreditCards.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.CreditCards;

[ApiController]
[Route("api/v1/credit-cards")]
[Authorize]
public sealed class CreditCardsController(CreditCardsService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreditCardResponse>> Create(
        CreateCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(request, GetHouseholdId(), GetUserId(), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditCardResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        // NotFoundException (doesn't exist, is inactive, or belongs to a different household
        // - the latter caught by the global query filter) is caught by
        // DomainExceptionHandler and mapped to a 404 ProblemDetails response.
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CreditCardResponse>> Update(
        Guid id,
        UpdateCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        // NotFoundException is also thrown when the authenticated user is a household member
        // but not an owner of the card - see CreditCardsService.GetOwnedOrThrowAsync.
        return Ok(await service.UpdateAsync(id, request, GetUserId(), cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, GetUserId(), cancellationToken);

        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetHouseholdId() => Guid.Parse(User.FindFirstValue("household_id")!);
}
