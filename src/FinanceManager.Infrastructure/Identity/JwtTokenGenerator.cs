using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceManager.Infrastructure.Identity;

public sealed class JwtTokenGenerator(IConfiguration configuration) : ITokenGenerator
{
    public string GenerateAccessToken(Guid userId, Guid? householdId, HouseholdRole? role)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Missing required configuration: Jwt:SigningKey");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Missing required configuration: Jwt:Issuer");

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };

        if (householdId is not null)
        {
            claims.Add(new Claim("household_id", householdId.Value.ToString()));
        }

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
