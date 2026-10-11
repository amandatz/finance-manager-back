using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Application.Users;

public sealed class AuthService(IIdentityService identityService, ITokenGenerator tokenGenerator)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var authenticated = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (authenticated is null)
        {
            // Deliberately generic: never reveal whether the email exists or the
            // password was wrong.
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var token = tokenGenerator.GenerateAccessToken(
            authenticated.UserId,
            authenticated.HouseholdId,
            authenticated.Role);

        return new LoginResponse(token);
    }
}
