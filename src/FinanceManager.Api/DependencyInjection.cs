using FinanceManager.Api.ErrorHandling;
using FinanceManager.Api.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>());

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
