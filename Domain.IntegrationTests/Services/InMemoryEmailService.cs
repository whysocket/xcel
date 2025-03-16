using System.Collections.Concurrent;
using Xcel.Services.Interfaces;
using Xcel.Services.Models;

namespace Domain.IntegrationTests.Services;

public class InMemoryEmailService : IEmailService
{
    private readonly ConcurrentBag<SentEmail> _emails = [];

    internal record SentEmail(
        object Payload,
        string Body);

    public Task SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default) where TData : class
    {
        _emails.Add(new SentEmail(payload, "mock"));

        return Task.CompletedTask;
    }

    internal IReadOnlyCollection<SentEmail> GetSentEmails()
    {
        return _emails.ToList().AsReadOnly();
    }
}
