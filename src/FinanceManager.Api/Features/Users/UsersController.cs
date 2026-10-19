using System.Security.Claims;
using FinanceManager.Application.Users;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Users;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(UsersService usersService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> GetMe(CancellationToken cancellationToken)
    {
        // AuthenticationFailedException (thrown when the token no longer resolves to a
        // user, or to a membership in the token's household) is caught by
        // DomainExceptionHandler and mapped to a 401 ProblemDetails response.
        return Ok(await usersService.GetCurrentUserAsync(GetUserId(), GetHouseholdId(), cancellationToken));
    }

    [HttpGet("me/households")]
    public async Task<ActionResult<IReadOnlyList<MembershipResponse>>> GetMyHouseholds(
        CancellationToken cancellationToken)
    {
        return Ok(await usersService.GetMembershipsAsync(GetUserId(), cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = nameof(HouseholdRole.Owner))]
    public async Task<ActionResult<CreatedUserResponse>> CreateMember(
        CreateMemberRequest request,
        CancellationToken cancellationToken)
    {
        // ConflictException (email already in use) and BusinessRuleViolationException
        // (password policy) are caught by DomainExceptionHandler and mapped to 409/400
        // ProblemDetails responses respectively.
        var response = await usersService.CreateMemberAsync(request, GetHouseholdId(), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("me/password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        // BusinessRuleViolationException (incorrect current password, or new password
        // fails policy) is caught by DomainExceptionHandler and mapped to a 400
        // ProblemDetails response.
        await usersService.ChangePasswordAsync(GetUserId(), request, cancellationToken);

        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetHouseholdId() => Guid.Parse(User.FindFirstValue("household_id")!);
}
