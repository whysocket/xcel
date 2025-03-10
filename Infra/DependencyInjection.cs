using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Infra.Repositories;
using Infra.Repositories.Shared;
using Infra.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        services
            .AddDbContext<AppDbContext>(o =>
            {
                o.UseNpgsql(infraOptions.Database.ConnectionString);
            })
            .AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))
            .AddScoped<ISubjectsRepository, SubjectsRepository>();

        services
            .AddEmailServices(infraOptions.Email);

        return services;
    }

    public static IServiceCollection AddEmailServices(
           this IServiceCollection services,
           EmailOptions emailOptions)
    {
        return services
           .AddSingleton(new SmtpClient(emailOptions.Host, emailOptions.Port) // Use options
           {
               Credentials = new System.Net.NetworkCredential(emailOptions.Username, emailOptions.Password),
               EnableSsl = emailOptions.EnableSsl
           })
           .AddScoped<IEmailService, TemplatedEmailService>(provider => new TemplatedEmailService(
               provider.GetRequiredService<SmtpClient>(),
               emailOptions.FromAddress,
               "EmailTemplates"));

    }
}
