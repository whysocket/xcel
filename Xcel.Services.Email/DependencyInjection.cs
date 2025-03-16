using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Xcel.Services.Implementations;
using Xcel.Services.Interfaces;

namespace Xcel.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddXcelEmailServices(
           this IServiceCollection services,
           EmailOptions emailOptions)
    {
        return services
           .AddSingleton(emailOptions)
           .AddSingleton(new SmtpClient(emailOptions.Host, emailOptions.Port)
           {
               Credentials = new System.Net.NetworkCredential(emailOptions.Username, emailOptions.Password),
               EnableSsl = emailOptions.EnableSsl
           })
           .AddScoped<IEmailSender, SmtpEmailSender>()
           .AddScoped<IEmailService, TemplatedEmailService>();
    }
}
