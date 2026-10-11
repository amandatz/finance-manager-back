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
        var response = await authService.LoginAsync(request, cancellationToken);

        return response is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.")
            : Ok(response);
    }
}
