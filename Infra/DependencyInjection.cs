using Application;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Domain.Interfaces.Services;
using Infra.Repositories;
using Infra.Repositories.Shared;
using Infra.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xcel.Services.Auth;
using Xcel.Services.Email;

namespace Infra;

interface IOptionsValidate
{
    void Validate(EnvironmentKind environment);
}
public class DatabaseOptions : IOptionsValidate
{
    public required string ConnectionString { get; set; }
    public DevPowersOptions? DevPowers { get; set; }

    public void Validate(EnvironmentKind environment)
    {
        if (environment != EnvironmentKind.Development && DevPowers != null)
        {
            throw new ArgumentException("DevPowers must be null outside of the Development environment.");
        }
    }
}

public class DevPowersOptions
{
    public DatabaseDevPower Recreate { get; set; } = DatabaseDevPower.None;
    public DatabaseDevPower Migrate { get; set; } = DatabaseDevPower.None;
}

public enum DatabaseDevPower
{
    None,
    Always
}

public class InfraOptions
{
    public required DatabaseOptions Database { get; set; }
    public required EmailOptions Email { get; set; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfraServices(
        this IServiceCollection services,
        InfraOptions infraOptions,
        EnvironmentKind environment)
    {
        infraOptions.Database.Validate(environment);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        services
            .AddApplicationServices()
            .AddDatabaseServices(infraOptions.Database)
            .AddXcelAuthServices<OtpRepository, PersonsRepository>()
            .AddXcelEmailServices(infraOptions.Email)
            .AddScoped<IFileService, LocalFileService>();

        return services;
    }

    private static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        DatabaseOptions databaseOptions)
    {
        services.AddSingleton(databaseOptions);

        services.AddDbContext<AppDbContext>(o =>
        {
            o.UseNpgsql(databaseOptions.ConnectionString);
        });

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISubjectsRepository, SubjectsRepository>();
        services.AddScoped<ITutorsRepository, TutorsRepository>();
        services.AddScoped<IPersonsRepository, PersonsRepository>();

        if (databaseOptions.DevPowers != null)
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (databaseOptions.DevPowers.Recreate == DatabaseDevPower.Always)
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }
            else if (databaseOptions.DevPowers.Migrate == DatabaseDevPower.Always)
            {
                dbContext.Database.Migrate();
            }
        }

        return services;
    }
}