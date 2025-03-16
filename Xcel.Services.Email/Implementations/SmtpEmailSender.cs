using System.Net.Mail;
using Xcel.Services.Interfaces;
using Xcel.Services.Models;

namespace Xcel.Services.Implementations;

public class SmtpEmailSender(SmtpClient smtpClient, EmailOptions options) : IEmailSender
{
    public async Task SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        string body,
        CancellationToken cancellationToken = default) where TData : class
    {
        var message = new MailMessage(options.FromAddress, payload.To, payload.Subject, body)
        {   
            IsBodyHtml = true
        };

        await smtpClient.SendMailAsync(message, cancellationToken);
    }
}
