using Domain.Interfaces.Services;
using Domain.Payloads.Email.Shared;
using RazorEngine;
using RazorEngine.Templating;
using System.Net.Mail;

namespace Infra.Services.Email;

public class WelcomeEmailTemplateData : ITemplateData
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}


public class TemplatedEmailService(SmtpClient smtpClient, string fromAddress, string templateDirectory) : IEmailService
{
    public async Task<bool> SendEmailAsync<T>(EmailPayload<T> payload) where T : ITemplateData
    {
        try
        {
            var templateName = typeof(T).Name.Replace("TemplateData", "").ToLower(); // To fix
            var templatePath = Path.Combine(templateDirectory, $"{templateName}.cshtml");
            var template = File.ReadAllText(templatePath);
            var body = Engine.Razor.RunCompile(template, templateName, null, payload.TemplateData);

            var message = new MailMessage(fromAddress, payload.To, payload.Subject, body)
            {
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(message);
            return true;
        }
        catch (SmtpException ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
            return false;
        }
        catch (TemplateCompilationException ex)
        {
            Console.WriteLine($"Template compilation failed: {ex.Message}");
            return false;
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Template file not found {ex.Message}");
            return false;
        }
    }
}