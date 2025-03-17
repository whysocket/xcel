using System.Net.Mail;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Microsoft.Extensions.Logging;

namespace Xcel.Services.Email.Implementations;

public class SmtpEmailSender(SmtpClient smtpClient, EmailOptions options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async ValueTask SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        CancellationToken cancellationToken = default) where TData : class
    {
        ArgumentNullException.ThrowIfNull(smtpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrEmpty(options.FromAddress))
        {
            throw new ArgumentException("FromAddress cannot be null or empty.", nameof(options.FromAddress));
        }

        if (string.IsNullOrEmpty(payload.To))
        {
            throw new ArgumentException("To cannot be null or empty.", nameof(payload.To));
        }

        if (string.IsNullOrEmpty(payload.Subject))
        {
            throw new ArgumentException("Subject cannot be null or empty.", nameof(payload.Subject));
        }

        try
        {
            var message = new MailMessage(options.FromAddress, payload.To, payload.Subject, payload.Body)
            {
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex, "Error sending email: {Message}", ex.Message);
            throw; // Rethrow the exception
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error sending email: {Message}", ex.Message);
            throw; // Rethrow the exception
        }
    }
}