using System.Security.Claims;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Users;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(IIdentityService identityService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var profile = await identityService.GetUserAsync(GetUserId(), cancellationToken);

        if (profile is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(
            profile.Id,
            profile.Name,
            profile.Email,
            profile.Role,
            profile.HouseholdId));
    }

    [HttpPost]
    [Authorize(Roles = nameof(HouseholdRole.Owner))]
    public async Task<ActionResult<CreatedUserResponse>> CreateMember(
        CreateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.CreateUserAsync(
            request.Name,
            request.Email,
            request.Password,
            GetHouseholdId(),
            HouseholdRole.Member,
            cancellationToken);

        return result.Outcome switch
        {
            CreateUserOutcome.Success => StatusCode(
                StatusCodes.Status201Created,
                new CreatedUserResponse(result.UserId!.Value, request.Name, request.Email)),
            CreateUserOutcome.EmailAlreadyInUse => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email already in use.",
            }),
            CreateUserOutcome.PasswordPolicyViolation => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["Password"] = [.. result.Errors] })),
            _ => Problem(),
        };
    }

    [HttpPost("me/password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await identityService.ChangePasswordAsync(
            GetUserId(),
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return outcome switch
        {
            ChangePasswordOutcome.Success => NoContent(),
            ChangePasswordOutcome.CurrentPasswordIncorrect => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The current password is incorrect.",
            }),
            ChangePasswordOutcome.NewPasswordPolicyViolation => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The new password does not meet the password policy.",
            }),
            _ => Problem(),
        };
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetHouseholdId() => Guid.Parse(User.FindFirstValue("household_id")!);
}
