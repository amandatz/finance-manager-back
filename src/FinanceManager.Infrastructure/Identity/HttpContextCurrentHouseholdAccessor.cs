using System.Security.Claims;
using FinanceManager.Application.Abstractions;
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
                throw new InvalidOperationException(
                    "No authenticated household context is available for the current request.");
            }

            return householdId;
        }
    }
}
