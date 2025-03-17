using Microsoft.Extensions.DependencyInjection;
using Xcel.Services.Auth.Implementations;
using Xcel.Services.Auth.Interfaces;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Xcel.Services.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddXcelAuthServices<TOtpRepository, TPersonRepository>(
        this IServiceCollection services)
        where TOtpRepository : class, IOtpRepository
        where TPersonRepository : class, IPersonsRepository
    {
        services.TryAddScoped<IOtpRepository, TOtpRepository>();
        services.TryAddScoped<IPersonsRepository, TPersonRepository>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}
