using Application;
using Application.Config;
using Infra;
using Infra.Options;
using Presentation.API.Options;

namespace Presentation.API;

public static class DependencyInjection
{
    public static async Task AddOptionsAndServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = configuration.GetRequiredSection("Api").Get<ApiOptions>()
            ?? throw new InvalidOperationException("It's mandatory to have the Api configuration");
        
        var infraOptions = configuration.GetRequiredSection("Infra").Get<InfraOptions>()
                           ?? throw new InvalidOperationException("It's mandatory to have the Infra configuration");

        var environment = new EnvironmentConfig(configuration.GetValue<EnvironmentType>("Environment"));

        await services.AddInfraServicesAsync(infraOptions, environment);

        services
            .AddSingleton(apiOptions)
            .AddSingleton(environment);
    }
}