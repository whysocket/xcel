using Domain.Interfaces.Services;
using Domain.Payloads.Email.Shared;
using HandlebarsDotNet;
using Infra.Interfaces.Services.Email;
using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace Infra.Services.Email;

public class TemplatedEmailService(IEmailSender emailSender, ILogger<TemplatedEmailService> logger) : IEmailService
{
    public async Task<bool> SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
    {
        try
        {
            var templateName = typeof(TData).Name.Replace("Data", "Template");
            var templatePath = Path.Combine("Services", "Email", "Templates", $"{templateName}.hbs");
            var templateContent = File.ReadAllText(templatePath);

            var template = Handlebars.Compile(templateContent);
            var body = template(payload.Data);

            await emailSender.SendEmailAsync(payload, body, cancellationToken);

            return true;
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex, "Email sending failed: {Message}", ex.Message);
            return false;
        }
        catch (HandlebarsCompilerException ex)
        {
            logger.LogError(ex, "Template compilation failed: {Message}", ex.Message);
            return false;
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Template file not found: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
            return false;
        }
    }
}