using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Mail;
using Xcel.Services.Email.Exceptions;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;

namespace Xcel.Services.Email.Implementations;

internal class TemplatedEmailService(IEmailSender emailSender, ILogger<TemplatedEmailService> logger) : IEmailService
{
    private const string TemplatesDirectory = "Templates";
    private const string TemplateFileExtension = ".hbs";
    private readonly ConcurrentDictionary<string, Func<object, string>> _templateCache = new();

    public async Task SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
    {
        try
        {
            var typeName = typeof(TData).Name;
            var templateName = typeName.Replace("Data", "Template");
            var templateDirectory = typeName.Replace("Data", "");
            var templatePath = Path.Combine(TemplatesDirectory, templateDirectory, $"{templateName}{TemplateFileExtension}");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file '{templatePath}' not found.");
            }

            payload.Body = await RenderTemplate(templatePath, payload.Data, cancellationToken);

            await emailSender.SendEmailAsync(payload, cancellationToken);
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

    private async Task<string> RenderTemplate(string templatePath, object data, CancellationToken cancellationToken)
    {
        Func<object, string> template;

        if (_templateCache.TryGetValue(templatePath, out var cachedTemplate))
        {
            template = cachedTemplate;
        }
        else
        {
            var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
            template = (d) => Handlebars.Compile(templateContent)(d).ToString();
            _templateCache.TryAdd(templatePath, template);
        }

        return await Task.Run(() => template(data).ToString(), cancellationToken);
    }
}