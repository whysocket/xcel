using Domain.Payloads.Email.Shared;

namespace Domain.Interfaces.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default)
        where TData : class;
}