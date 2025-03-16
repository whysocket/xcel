using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Domain.Interfaces.Services;
using Infra.Repositories;
using Infra.Repositories.Shared;
using Infra.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xcel.Services;

namespace Infra;

public class DatabaseOptions
{
    public required string ConnectionString { get; set; }
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
           InfraOptions infraOptions)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console() // Log to console (you can add other sinks like file, database, etc.)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        services
            .AddDatabaseServices(infraOptions.Database)
            .AddXcelEmailServices(infraOptions.Email)
            .AddScoped<IAccountService, AccountService>()
            .AddScoped<IFileService, LocalFileService>();

        return services;
    }

    private static IServiceCollection AddDatabaseServices(
             this IServiceCollection services,
             DatabaseOptions databaseOptions)
    {
        return services
            .AddSingleton(databaseOptions)
            .AddDbContext<AppDbContext>(o =>
            {
                o.UseNpgsql(databaseOptions.ConnectionString);
            })
            .AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))
            .AddScoped<ISubjectsRepository, SubjectsRepository>()
            .AddScoped<ITutorsRepository, TutorsRepository>()
            .AddScoped<IPersonsRepository, PersonsRepository>();
    }
}
