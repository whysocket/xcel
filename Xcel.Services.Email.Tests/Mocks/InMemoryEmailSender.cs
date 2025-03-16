using HandlebarsDotNet;
using System.Collections.Concurrent;
using System.Net.Mail;
using Xcel.Services.Interfaces;
using Xcel.Services.Models;

namespace Xcel.Services.Email.Tests.Mocks;

public class InMemoryEmailSender : IEmailSender
{
    public enum SimulationType
    {
        None,
        SmtpException,
        HandlebarsCompilerException,
        GenericException
    }

    public SimulationType Simulation { get; set; } = SimulationType.None;

    public record SentEmail(object Payload, string Body);

    private readonly ConcurrentBag<SentEmail> _sentEmails = [];

    public Task SendEmailAsync<TData>(EmailPayload<TData> payload, string body, CancellationToken cancellationToken = default) where TData : class
    {
        switch (Simulation)
        {
            case SimulationType.SmtpException:
                throw new SmtpException("Simulated SMTP error");
            case SimulationType.HandlebarsCompilerException:
                throw new HandlebarsCompilerException("Simulated Handlebars compilation error");
            case SimulationType.GenericException:
                throw new Exception("Simulated generic error");
        }

        _sentEmails.Add(new SentEmail(payload, body));

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<SentEmail> GetSentEmails()
    {
        return [.. _sentEmails];
    }

    public void ClearSentEmails()
    {
        _sentEmails.Clear();
    }
}