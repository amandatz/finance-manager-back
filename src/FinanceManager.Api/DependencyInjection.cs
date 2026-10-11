using System.Text;
using FinanceManager.Api.ErrorHandling;
using FinanceManager.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FinanceManager.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>());

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Read lazily, at the point the options are actually resolved (first
                // authenticated request), not eagerly here - eagerly reading from
                // `configuration` captures whatever was in the stack at this call site,
                // which in WebApplicationFactory-based tests runs before the test's
                // ConfigureAppConfiguration override is layered in, causing issuer/key
                // used for validation to silently differ from the one used to issue tokens.
                var signingKey = configuration["Jwt:SigningKey"]
                    ?? throw new InvalidOperationException("Missing required configuration: Jwt:SigningKey");
                var issuer = configuration["Jwt:Issuer"]
                    ?? throw new InvalidOperationException("Missing required configuration: Jwt:Issuer");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
