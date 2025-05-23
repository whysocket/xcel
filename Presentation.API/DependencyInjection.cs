﻿using Infra;
using Presentation.API.Options;
using Xcel.Config.Options;

namespace Presentation.API;

public static class DependencyInjection
{
    internal static EnvironmentOptions AddEnvironmentOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var environmentOptions = new EnvironmentOptions(
            configuration.GetValue<EnvironmentType>("Environment")
        );

        services.AddSingleton(environmentOptions);

        return environmentOptions;
    }

    public static ApiOptions AddApiOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var apiOptions = configuration.GetOptions<ApiOptions>();

        services.AddSingleton(apiOptions);

        return apiOptions;
    }

    public static InfraOptions AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration,
        EnvironmentOptions environment
    )
    {
        var infraOptions = configuration.GetOptions<InfraOptions>();

        services.AddInfraServicesAsync(infraOptions, environment);

        return infraOptions;
    }
}
