using System.Security.Claims;
using FinanceManager.Application.Abstractions;
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
        var profile = await usersService.GetCurrentUserAsync(GetUserId(), cancellationToken);

        return profile is null ? Unauthorized() : Ok(profile);
    }

    [HttpPost]
    [Authorize(Roles = nameof(HouseholdRole.Owner))]
    public async Task<ActionResult<CreatedUserResponse>> CreateMember(
        CreateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await usersService.CreateMemberAsync(request, GetHouseholdId(), cancellationToken);

        return result.Outcome switch
        {
            CreateUserOutcome.Success => StatusCode(
                StatusCodes.Status201Created,
                new CreatedUserResponse(result.UserId!.Value, request.Name, request.Email)),
            CreateUserOutcome.EmailAlreadyInUse => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already in use."),
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
        var outcome = await usersService.ChangePasswordAsync(GetUserId(), request, cancellationToken);

        return outcome switch
        {
            ChangePasswordOutcome.Success => NoContent(),
            ChangePasswordOutcome.CurrentPasswordIncorrect => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The current password is incorrect."),
            ChangePasswordOutcome.NewPasswordPolicyViolation => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The new password does not meet the password policy."),
            _ => Problem(),
        };
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetHouseholdId() => Guid.Parse(User.FindFirstValue("household_id")!);
}
