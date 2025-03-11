using Domain.Interfaces.Services;
using Domain.Payloads.Email.Shared;
using System.Collections.Concurrent;

namespace Domain.IntegrationTests.Services;

public class InMemoryEmailService : IEmailService
{
    private readonly ConcurrentBag<SentEmail> _emails = [];

    internal record SentEmail(
        object Payload,
        string Body);

    public Task<bool> SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
    {
        _emails.Add(new SentEmail(payload, "mock"));
        return Task.FromResult(true);
    }

    internal IReadOnlyCollection<SentEmail> GetSentEmails()
    {
        return _emails.ToList().AsReadOnly();
    }
}
