using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Xcel.Services.Email.Implementations;
using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email;

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
                Credentials = new System.Net.NetworkCredential(emailOptions.FromAddress, emailOptions.Password),
                EnableSsl = emailOptions.EnableSsl
            })
            .AddScoped<IEmailSender, SmtpEmailSender>()
            .AddScoped<IEmailService, TemplatedEmailService>();
    }
}