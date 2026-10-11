using FinanceManager.Application.FinancialAccounts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        services.AddScoped<FinancialAccountsService>();

        return services;
    }
}
