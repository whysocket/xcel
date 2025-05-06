using System.Collections.Concurrent;
using Domain.Results;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;

namespace Xcel.TestUtils.Mocks.XcelServices.Email;

public class InMemoryEmailService : IEmailService
{
    public record SentEmail<TData>(EmailPayload<TData> Payload)
        where TData : IEmail;

    private readonly ConcurrentBag<object> _sentEmails = [];

    public Task<Result> SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        CancellationToken cancellationToken = default
    )
        where TData : IEmail
    {
        _sentEmails.Add(new SentEmail<TData>(payload));

        return Task.FromResult(Result.Ok());
    }

    public IReadOnlyList<SentEmail<TData>> GetSentEmails<TData>()
        where TData : IEmail
    {
        return _sentEmails.OfType<SentEmail<TData>>().ToList();
    }

    public SentEmail<TData> GetSentEmail<TData>()
        where TData : IEmail
    {
        var sentEmail = _sentEmails.OfType<SentEmail<TData>>().FirstOrDefault();
        if (sentEmail == null || sentEmail.Equals(null))
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
