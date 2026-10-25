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
    /// <summary>
    /// Name of the CORS policy applied to the whole API. Kept as a constant so
    /// <c>Program.cs</c> can reference the same policy it configures here without
    /// repeating the literal.
    /// </summary>
    public const string CorsPolicyName = "FrontendCors";

    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>());

        services.AddProblemDetails();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                // Allowed origins are configuration-driven (Cors:AllowedOrigins), not
                // hardcoded, so each environment (dev, staging, prod) can list its own
                // frontend origin(s) without a code change. Falls back to the Next.js
                // dev server default so a clean checkout works out of the box.
                //
                // Get<string[]>() on an empty JSON array ("AllowedOrigins": []) returns an
                // empty array, not null - appsettings.json's base config uses exactly that
                // empty array, so the null-check alone (`?? [...]`) never triggers and the
                // policy silently ends up with no allowed origins. Both cases must be
                // checked explicitly.
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                if (allowedOrigins is null || allowedOrigins.Length == 0)
                    allowedOrigins = ["http://localhost:3000"];

                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Order matters: IExceptionHandler implementations are tried in registration
        // order, and the first one whose TryHandleAsync returns true wins. Domain
        // exceptions (expected, client-facing outcomes) are handled first; anything else
        // falls through to GlobalExceptionHandler's generic 500.
        services.AddExceptionHandler<DomainExceptionHandler>();
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
