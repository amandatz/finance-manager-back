using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;

namespace FinanceManager.Application.Users;

public sealed class AuthService(IIdentityService identityService, ITokenGenerator tokenGenerator)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var authenticated = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (authenticated is null)
        {
            return null;
        }

        var token = tokenGenerator.GenerateAccessToken(
            authenticated.UserId,
            authenticated.HouseholdId,
            authenticated.Role);

        return new LoginResponse(token);
    }
}
