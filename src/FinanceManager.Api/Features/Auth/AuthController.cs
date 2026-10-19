using System.Security.Claims;
using FinanceManager.Application.Users;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        // AuthenticationFailedException (invalid email or password) is caught by
        // DomainExceptionHandler and mapped to a 401 ProblemDetails response.
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // ConflictException (email already in use) and BusinessRuleViolationException
        // (invalid invite code, or a rejected password) are caught by
        // DomainExceptionHandler and mapped to 409/400 ProblemDetails responses.
        var response = await authService.RegisterAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        // AuthenticationFailedException (unknown, expired, or replayed token) is caught by
        // DomainExceptionHandler and mapped to a 401 ProblemDetails response.
        return Ok(await authService.RefreshAsync(request, cancellationToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPost("switch-household")]
    [Authorize]
    public async Task<ActionResult<LoginResponse>> SwitchHousehold(
        SwitchHouseholdRequest request,
        CancellationToken cancellationToken)
    {
        // NotFoundException (no membership in the target household) is caught by
        // DomainExceptionHandler and mapped to a 404 ProblemDetails response.
        return Ok(await authService.SwitchHouseholdAsync(GetUserId(), request.HouseholdId, cancellationToken));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
