using System.Security.Claims;
using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace FinanceManager.Infrastructure.Identity;

/// <summary>
/// Reads the current household id from the <c>household_id</c> claim on the current <see cref="HttpContext"/>'s <c>User</c>.
/// </summary>
public sealed class HttpContextCurrentHouseholdAccessor(IHttpContextAccessor httpContextAccessor)
    : ICurrentHouseholdAccessor
{
    public Guid HouseholdId
    {
        get
        {
            var claimValue = httpContextAccessor.HttpContext?.User.FindFirstValue("household_id");

            if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var householdId))
            {
                // The household_id claim is optional on the token (a user with no active
                // household, e.g. removed from every household, gets one without it) - this
                // is an expected, client-facing outcome, not a server bug, so it maps to 401
                // rather than falling through to GlobalExceptionHandler as a 500.
                throw new AuthenticationFailedException("No active household for the current user.");
            }

            return householdId;
        }
    }
}
