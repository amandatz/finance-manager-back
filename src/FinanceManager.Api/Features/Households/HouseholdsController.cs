using System.Security.Claims;
using FinanceManager.Application.Households;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Households;

[ApiController]
[Route("api/v1/households")]
[Authorize]
public sealed class HouseholdsController(HouseholdsService householdsService) : ControllerBase
{
    [HttpPost("invite-code")]
    [Authorize(Roles = nameof(HouseholdRole.Owner))]
    public async Task<ActionResult<InviteCodeResponse>> GenerateInviteCode(CancellationToken cancellationToken)
    {
        return Ok(await householdsService.GenerateInviteCodeAsync(GetHouseholdId(), cancellationToken));
    }

    [HttpPost("join")]
    public async Task<ActionResult<LoginResponse>> Join(
        JoinHouseholdRequest request,
        CancellationToken cancellationToken)
    {
        // BusinessRuleViolationException (invalid code) and ConflictException (already a
        // member) are caught by DomainExceptionHandler and mapped to 400/409 ProblemDetails
        // responses.
        return Ok(await householdsService.JoinAsync(GetUserId(), request.InviteCode, cancellationToken));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetHouseholdId() => Guid.Parse(User.FindFirstValue("household_id")!);
}
