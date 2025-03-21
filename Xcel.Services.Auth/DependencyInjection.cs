using Microsoft.Extensions.DependencyInjection;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;

namespace Xcel.Services.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddXcelAuthServices<TOtpRepository, TPersonRepository>(
        this IServiceCollection services,
        AuthOptions authOptions)
        where TOtpRepository : class, IOtpRepository
        where TPersonRepository : class, IPersonsRepository
    {
        services.TryAddScoped<IOtpRepository, TOtpRepository>();
        services.TryAddScoped<IPersonsRepository, TPersonRepository>();
        services.TryAddSingleton(TimeProvider.System);

        services
            .AddScoped<IOtpService, OtpService>()
            .AddScoped<IAccountService, AccountService>()
            .AddSingleton<IJwtService, JwtService>();

        services
            .AddSingleton(authOptions);

        return services;
    }
}