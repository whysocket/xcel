using Infra;
using Infra.Options;
using Presentation.API.Options;
using Xcel.Config.Options;

namespace Presentation.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = configuration.GetOptions<ApiOptions>();
        
        services.AddSingleton(apiOptions);
        
        return services;
    }

    public static async Task AddExternalServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        EnvironmentOptions environment)
    {
        var infraOptions = configuration.GetOptions<InfraOptions>();

        await services.AddInfraServicesAsync(
            infraOptions,
            environment);
    }
}