using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using Xcel.Services.Exceptions;
using Xcel.Services.Interfaces;
using Xcel.Services.Models;

namespace Xcel.Services.Implementations;

public class TemplatedEmailService(IEmailSender emailSender, ILogger<TemplatedEmailService> logger) : IEmailService
{
    private const string TemplatesDirectory = "Templates";
    private const string TemplateFileExtension = ".hbs";

    public async Task SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
    {
        try
        {
            var typeName = typeof(TData).Name;
            var templateName = typeName.Replace("Data", "Template");
            var templateDirectory = typeName.Replace("Data", "");
            var templatePath = Path.Combine(TemplatesDirectory, templateDirectory, $"{templateName}{TemplateFileExtension}");

            var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
            var template = Handlebars.Compile(templateContent);
            var body = template(payload.Data);

            await emailSender.SendEmailAsync(payload, body, cancellationToken);
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex, "Email sending failed: {Message}", ex.Message);
            throw new EmailServiceException("Failed to send email due to SMTP error.", EmailServiceFailureReason.SmtpConnectionFailed, ex);
        }
        catch (HandlebarsCompilerException ex)
        {
            logger.LogError(ex, "Template compilation failed: {Message}", ex.Message);
            throw new EmailServiceException("Failed to compile email template.", EmailServiceFailureReason.TemplateCompilationError, ex);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Template file not found: {Message}", ex.Message);
            throw new EmailServiceException("Email template file not found.", EmailServiceFailureReason.TemplateFileNotFound, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex, "Template directory not found: {Message}", ex.Message);
            throw new EmailServiceException("Email template directory not found.", EmailServiceFailureReason.TemplateDirectoryNotFound, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
            throw new EmailServiceException("An unexpected error occurred while sending email.", EmailServiceFailureReason.UnknownError, ex);
        }
    }
}