using Application;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Domain.Interfaces.Services;
using Infra.Options;
using Infra.Repositories;
using Infra.Repositories.Auth;
using Infra.Repositories.Shared;
using Infra.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Xcel.Config.Options;
using Xcel.Services.Auth;
using Xcel.Services.Email;

namespace Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraServicesAsync(
        this IServiceCollection services,
        InfraOptions infraOptions,
        EnvironmentOptions environment
    )
    {
        services.TryAddSingleton(infraOptions);
        infraOptions.Validate(environment);

        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(logger, dispose: true);
        });

        services
            .AddApplicationServices()
            .AddXcelEmailServices(infraOptions.Email)
            .AddXcelAuthServices<
                OtpRepository,
                PersonsRepository,
                RolesRepository,
                PersonRoleRepository,
                RefreshTokensRepository
            >(infraOptions.Auth)
            .AddScoped<IFileService, LocalFileService>();

        AddDatabaseServices(services, infraOptions.Database);

        return services;
    }

    private static void AddDatabaseServices(
        this IServiceCollection services,
        DatabaseOptions databaseOptions
    )
    {
        services.AddSingleton(databaseOptions);

        services.AddDbContext<AppDbContext>(o =>
        {
            o.UseNpgsql(databaseOptions.ConnectionString);
        });

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISubjectsRepository, SubjectsRepository>();
        services.AddScoped<ITutorApplicationsRepository, TutorApplicationsRepository>();
        services.AddScoped<ITutorDocumentsRepository, TutorDocumentRepository>();
        services.AddScoped<IPersonsRepository, PersonsRepository>();
        services.AddScoped<ITutorProfilesRepository, TutorProfilesRepository>();
        services.AddScoped<IAvailabilityRulesRepository, AvailabilityRulesRepository>();
    }
}