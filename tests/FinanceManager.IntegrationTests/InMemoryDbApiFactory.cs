using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests;

/// <summary>
/// Swaps <see cref="FinanceManagerDbContext"/> onto the EF Core InMemory provider for every
/// test factory. Subclasses customize via <see cref="ConfigureAppConfiguration"/> and
/// <see cref="ConfigureServices"/> instead of overriding <see cref="ConfigureWebHost"/> directly,
/// so the provider-swap (easy to get subtly wrong - see its comment) lives in exactly one place.
/// </summary>
public abstract class InMemoryDbApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected sealed override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ConfigureAppConfiguration(builder);

        builder.ConfigureServices(services =>
        {
            SwapToInMemoryDatabase(services, _databaseName);
            ConfigureServices(services);
        });
    }

    protected virtual void ConfigureAppConfiguration(IWebHostBuilder builder)
    {
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    private static void SwapToInMemoryDatabase(IServiceCollection services, string databaseName)
    {
        // AddDbContext composes every registered IDbContextOptionsConfiguration<T> rather
        // than replacing it, so the real UseNpgsql(...) call from AddInfrastructure() must be
        // removed explicitly - removing only DbContextOptions<FinanceManagerDbContext> leaves
        // it in place and both providers end up configured on the same options, which EF rejects.
        var descriptorsToRemove = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<FinanceManagerDbContext>) ||
                d.ServiceType == typeof(FinanceManagerDbContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(FinanceManagerDbContext)))
            .ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<FinanceManagerDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
    }
}
