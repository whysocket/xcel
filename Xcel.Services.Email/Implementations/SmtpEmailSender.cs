using System.Net.Mail;
using Domain.Results;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Microsoft.Extensions.Logging;

namespace Xcel.Services.Email.Implementations;

internal class SmtpEmailSender(SmtpClient smtpClient, EmailOptions options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task<Result> SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        CancellationToken cancellationToken = default) where TData : class
    {
        if (string.IsNullOrEmpty(options.FromAddress))
        {
            logger.LogError("FromAddress cannot be null or empty.");
            return Result.Fail(new Error(ErrorType.Unexpected, "FromAddress cannot be null or empty."));
        }

        if (!payload.To.Any())
        {
            logger.LogError("To cannot be null or empty.");
            return Result.Fail(new Error(ErrorType.Unexpected, "To cannot be null or empty."));
        }

        if (string.IsNullOrEmpty(payload.Subject))
        {
            logger.LogError("Subject cannot be null or empty.");
            return Result.Fail(new Error(ErrorType.Unexpected, "Subject cannot be null or empty."));
        }

        try
        {
            var message = new MailMessage
            {
                From = new MailAddress(options.FromAddress),
                Subject = payload.Subject,
                Body = payload.Body,
                IsBodyHtml = true
            };

            foreach (var recipient in payload.To)
            {
                message.To.Add(recipient);
            }

            await smtpClient.SendMailAsync(message, cancellationToken);

            return Result.Ok();
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex, "Error sending email: {Message}", ex.Message);
            return Result.Fail(new Error(ErrorType.Unexpected, $"Error sending email: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error sending email: {Message}", ex.Message);
            return Result.Fail(new Error(ErrorType.Unexpected, $"Unexpected error sending email: {ex.Message}"));
        }
    }
}