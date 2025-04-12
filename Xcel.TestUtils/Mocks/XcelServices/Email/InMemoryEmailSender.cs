using System.Collections.Concurrent;
using System.Net.Mail;
using Domain.Results;
using HandlebarsDotNet;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;

namespace Xcel.TestUtils.Mocks.XcelServices.Email;

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

    public record struct SentEmail<TData>(EmailPayload<TData> Payload) where TData : class;

    private readonly ConcurrentBag<object> _sentEmails = [];

    public Task<Result> SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
    {
        switch (Simulation)
        {
            case SimulationType.SmtpException:
                throw new SmtpException("Simulated SMTP error");
            case SimulationType.HandlebarsCompilerException:
                throw new HandlebarsCompilerException("Simulated Handlebars compilation error");
            case SimulationType.GenericException:
                throw new Exception("Simulated generic error");
            case SimulationType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _sentEmails.Add(new SentEmail<TData>(payload));

        return Task.FromResult(Result.Ok());
    }

    public IReadOnlyList<SentEmail<TData>> GetSentEmails<TData>() where TData : class
    {
        return _sentEmails.OfType<SentEmail<TData>>().ToList();
    }

    public SentEmail<TData> GetSentEmail<TData>() where TData : class
    {
        var sentEmail = _sentEmails.OfType<SentEmail<TData>>().FirstOrDefault();
        if (sentEmail.Equals(default))
        {
            throw new InvalidOperationException($"No SentEmail<{typeof(TData).Name}> found.");
        }

        return sentEmail;
    }

    public void ClearSentEmails()
    {
        _sentEmails.Clear();
    }
}