using Domain.Payloads.Email.Shared;

namespace Infra.Interfaces.Services.Email;

public interface IEmailSender
{
    Task SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        string body,
        CancellationToken cancellationToken = default) where TData : class;
}