using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IIdentityService identityService,
    ITokenGenerator tokenGenerator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var authenticated = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (authenticated is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");
        }

        var token = tokenGenerator.GenerateAccessToken(
            authenticated.UserId,
            authenticated.HouseholdId,
            authenticated.Role);

        return Ok(new LoginResponse(token));
    }
}
