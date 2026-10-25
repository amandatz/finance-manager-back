using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Users.Requests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FinanceManager.IntegrationTests.Cors;

/// <summary>
/// Verifies the CORS policy configured in <c>FinanceManager.Api.DependencyInjection</c>
/// actually lets a browser running the frontend call the API cross-origin. This is the
/// regression test for the QA-reported bug where login worked via curl but failed in a real
/// browser with "No 'Access-Control-Allow-Origin' header is present" on the preflight.
/// </summary>
public class CorsTests : IDisposable
{
    private const string Route = "/api/v1/auth/login";
    private const string AllowedOrigin = "http://localhost:3000";
    private const string DisallowedOrigin = "http://evil.example.com";

    private readonly CorsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Preflight_ForLoginFromFrontendOrigin_ReturnsAllowedOriginAndMethod()
    {
        var client = _factory.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, Route);
        preflight.Headers.Add("Origin", AllowedOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(preflight);

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent,
            $"Expected a successful preflight, got {response.StatusCode}");
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains(
            response.Headers.GetValues("Access-Control-Allow-Methods").SelectMany(v => v.Split(',')),
            m => m.Trim().Equals("POST", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ActualRequest_FromFrontendOrigin_IncludesAllowOriginHeader()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, Route)
        {
            Content = JsonContent.Create(new LoginRequest("nobody@test.local", "WrongPassword123")),
        };
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Preflight_FromDisallowedOrigin_DoesNotReceiveAllowOriginHeader()
    {
        var client = _factory.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, Route);
        preflight.Headers.Add("Origin", DisallowedOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(preflight);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    /// <summary>
    /// Explicitly configures <c>Cors:AllowedOrigins</c> to a known value rather than relying on
    /// <c>DependencyInjection</c>'s hardcoded fallback, so this test keeps passing regardless of
    /// what that fallback is.
    /// </summary>
    private sealed class CorsApiFactory : InMemoryDbApiFactory
    {
        protected override void ConfigureAppConfiguration(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-characters-long-for-hmac",
                    ["Jwt:Issuer"] = "FinanceManager.Tests",
                    ["Cors:AllowedOrigins:0"] = AllowedOrigin,
                });
            });
        }
    }
}

/// <summary>
/// Regression test for the bug that escaped the original <see cref="CorsTests"/> coverage: the
/// real <c>appsettings.json</c> ships <c>"Cors": { "AllowedOrigins": [] }</c> - an empty array,
/// not an absent section - and <c>IConfiguration.Get&lt;string[]&gt;()</c> on an empty JSON array
/// returns an empty array rather than <c>null</c>. <c>DependencyInjection</c> originally only
/// null-checked the result (<c>?? ["http://localhost:3000"]</c>), so that fallback never
/// triggered and the CORS policy was registered with zero allowed origins - every test in
/// <see cref="CorsTests"/> explicitly injects <c>Cors:AllowedOrigins:0</c>, so none of them ever
/// exercised this path. This class reproduces the real config (empty array, no explicit origin)
/// and asserts the hardcoded <c>http://localhost:3000</c> fallback is applied.
/// </summary>
public class CorsEmptyConfigurationTests : IDisposable
{
    private const string Route = "/api/v1/auth/login";
    private const string FallbackOrigin = "http://localhost:3000";

    private readonly EmptyCorsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Preflight_WithEmptyAllowedOriginsConfiguration_FallsBackToLocalhost3000()
    {
        var client = _factory.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, Route);
        preflight.Headers.Add("Origin", FallbackOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(preflight);

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent,
            $"Expected a successful preflight, got {response.StatusCode}");
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(FallbackOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    /// <summary>
    /// Configures <c>Cors:AllowedOrigins</c> as an empty array - matching the real
    /// <c>appsettings.json</c> - instead of either omitting the key or injecting an explicit
    /// origin, so this factory exercises the empty-array fallback path directly.
    /// </summary>
    private sealed class EmptyCorsApiFactory : InMemoryDbApiFactory
    {
        protected override void ConfigureAppConfiguration(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-characters-long-for-hmac",
                    ["Jwt:Issuer"] = "FinanceManager.Tests",
                });
                // AddInMemoryCollection can't represent an empty array directly, so the
                // section is added as JSON to reproduce `"AllowedOrigins": []` exactly as it
                // appears in the real appsettings.json.
                config.AddJsonStream(new MemoryStream(
                    System.Text.Encoding.UTF8.GetBytes("""{"Cors":{"AllowedOrigins":[]}}""")));
            });
        }
    }
}
