using System.Collections.Concurrent;
using System.Net.Mail;
using HandlebarsDotNet;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;

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

    public record struct SentEmail<TData>(EmailPayload<TData> Payload) where TData : class { }

    private readonly ConcurrentBag<object> _sentEmails = new();

    public ValueTask SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
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

        _sentEmails.Add(new SentEmail<TData>(payload));

        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<SentEmail<TData>> GetSentEmails<TData>() where TData : class
    {
        return _sentEmails.OfType<SentEmail<TData>>().ToList();
    }

    public SentEmail<TData> GetSentEmail<TData>() where TData : class
    {
        var sentEmail = _sentEmails.FirstOrDefault(); 

        if (sentEmail == null)
        {
            return default; 
        }

        if (sentEmail is SentEmail<TData> typedSentEmail)
        {
            return typedSentEmail;
        }

        throw new InvalidOperationException($"No SentEmail<{typeof(TData).Name}> found.");
    }

    public void ClearSentEmails()
    {
        _sentEmails.Clear();
    }
}