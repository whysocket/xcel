using Microsoft.Extensions.DependencyInjection;

namespace Xcel.Services.Email;

public static class DependencyInjection
{
    public static IServiceCollection AddXcelEmailServices(
        this IServiceCollection services,
        EmailOptions emailOptions)
    {
        return services
            .AddSingleton(emailOptions);
    }
}