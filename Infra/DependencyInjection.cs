using Application;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Domain.Interfaces.Services;
using Infra.Extensions.DbContext;
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
    public static async Task AddInfraServicesAsync(this IServiceCollection services,
        InfraOptions infraOptions,
        EnvironmentOptions environment)
    {
        services.TryAddSingleton(infraOptions);
        infraOptions.Validate(environment);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        services
            .AddApplicationServices()
            .AddXcelEmailServices(infraOptions.Email)
            .AddXcelAuthServices<OtpRepository, PersonsRepository, RolesRepository, PersonRoleRepository, RefreshTokensRepository>(infraOptions.Auth)
            .AddScoped<IFileService, LocalFileService>();

        await services.AddDatabaseServicesAsync(infraOptions.Database);
    }

    private static async Task AddDatabaseServicesAsync(
        this IServiceCollection services,
        DatabaseOptions databaseOptions)
    {
        services.AddSingleton(databaseOptions);

        services.AddDbContext<AppDbContext>(o => { o.UseNpgsql(databaseOptions.ConnectionString); });

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISubjectsRepository, SubjectsRepository>();
        services.AddScoped<ITutorApplicationsRepository, TutorApplicationsRepository>();
        services.AddScoped<ITutorDocumentsRepository, TutorDocumentRepository>();
        services.AddScoped<IPersonsRepository, PersonsRepository>();
        services.AddScoped<ITutorProfilesRepository, TutorProfilesRepository>();

        await MigrateOrRecreateDatabaseAsync(services, databaseOptions);
    }

    private static async Task MigrateOrRecreateDatabaseAsync(
        IServiceCollection services,
        DatabaseOptions databaseOptions)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (databaseOptions.DevPowers?.Recreate == true)
        {
            Log.Logger.Information("[Infra] Attempting to delete the database.");
            await dbContext.Database.EnsureDeletedAsync();
            Log.Logger.Information("[Infra] Database deleted successfully.");
            Log.Logger.Information("[Infra] Attempting to create the database.");
            await dbContext.Database.EnsureCreatedAsync();
            Log.Logger.Information("[Infra] Database created successfully.");
        }
        
        if (databaseOptions.DevPowers?.Migrate == DatabaseDevPower.Always)
        {
            try
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                foreach (var pendingMigration in pendingMigrations)
                {
                    Log.Logger.Information("[Infra] Applying pending database migrations.");
                    await dbContext.Database.MigrateAsync(pendingMigration);
                    Log.Logger.Information("[Infra] Database migrations applied successfully.");
                }

                Log.Logger.Information("[Infra] Database migrations applied....");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "[Infra] An error occurred while migrating the database.");
                throw;
            }
        }

        if (databaseOptions.DevPowers?.Seed == true)
        {
            Log.Logger.Information("[Infra] Database seeding initiated.");
            await dbContext.SeedAsync();
            Log.Logger.Information("[Infra] Database seeding finished.");
        }
    }
}

