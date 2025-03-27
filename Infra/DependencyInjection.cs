using Application;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Domain.Interfaces.Services;
using Infra.Options;
using Infra.Repositories;
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
            .AddXcelAuthServices<OtpRepository, PersonsRepository, RolesRepository, PersonRoleRepository>(infraOptions.Auth)
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
        services.AddScoped<ITutorsRepository, TutorsRepository>();
        services.AddScoped<IPersonsRepository, PersonsRepository>();

        await MigrateOrRecreateDatabaseAsync(services, databaseOptions);
    }

    private static async Task MigrateOrRecreateDatabaseAsync(
        IServiceCollection services,
        DatabaseOptions databaseOptions)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (databaseOptions.DevPowers?.Recreate == DatabaseDevPower.Always)
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            Log.Logger.Information("[Infra] Migrating database");
        }
        else if (databaseOptions.DevPowers?.Migrate == DatabaseDevPower.Always)
        {
            try
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                foreach (var pendingMigration in pendingMigrations)
                {
                    await dbContext.Database.MigrateAsync(pendingMigration);
                }

                Log.Logger.Information("[Infra] Database migrations applied....");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "[Infra] An error occurred while migrating the database.");
                throw;
            }
        }
    }
}