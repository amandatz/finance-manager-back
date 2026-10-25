using System.Security.Claims;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.FinancialAccounts;
using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.FinancialAccounts;

[ApiController]
[Route("api/v1/financial-accounts")]
[Authorize]
public sealed class FinancialAccountsController(
    FinancialAccountsService service,
    ICurrentHouseholdAccessor currentHouseholdAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FinancialAccountResponse>> Create(
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(request, GetHouseholdId(), GetUserId(), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FinancialAccountResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FinancialAccountResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        // NotFoundException (thrown when the account doesn't exist, is inactive, or belongs
        // to a different household - the latter caught by the global query filter) is
        // caught by DomainExceptionHandler and mapped to a 404 ProblemDetails response.
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FinancialAccountResponse>> Update(
        Guid id,
        UpdateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        // NotFoundException is also thrown (see FinancialAccountsService.GetOwnedOrThrowAsync)
        // when the authenticated user is a household member but not an owner of the account -
        // this project has no 403 precedent, so non-owner mutation attempts are
        // indistinguishable from "doesn't exist" to the caller.
        return Ok(await service.UpdateAsync(id, request, GetUserId(), cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, GetUserId(), cancellationToken);

        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetHouseholdId() => currentHouseholdAccessor.HouseholdId;
}
