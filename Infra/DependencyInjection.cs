using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Domain.Interfaces.Services;
using Infra.Interfaces.Services.Email;
using Infra.Repositories;
using Infra.Repositories.Shared;
using Infra.Services;
using Infra.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Mail;

namespace Infra;

public class DatabaseOptions
{
    public required string ConnectionString { get; set; }
}

public class EmailOptions
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromAddress { get; set; }
    public required bool EnableSsl { get; set; }
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
            .AddEmailServices(infraOptions.Email)
            .AddScoped<IAccountService, AccountService>();

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

    private static IServiceCollection AddEmailServices(
           this IServiceCollection services,
           EmailOptions emailOptions)
    {
        return services
           .AddSingleton(emailOptions)
           .AddSingleton(new SmtpClient(emailOptions.Host, emailOptions.Port) // Use options
           {
               Credentials = new System.Net.NetworkCredential(emailOptions.Username, emailOptions.Password),
               EnableSsl = emailOptions.EnableSsl
           })
           .AddScoped<IEmailSender, SmtpEmailSender>()
           .AddScoped<IEmailService, TemplatedEmailService>()
           .AddScoped<IFileService, LocalFileService>();

    }
}
