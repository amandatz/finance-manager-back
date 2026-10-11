using FinanceManager.Application.Users;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
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
}
