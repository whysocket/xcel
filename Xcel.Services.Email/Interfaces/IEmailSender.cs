using Xcel.Services.Email.Models;

namespace Xcel.Services.Email.Interfaces;

public interface IEmailSender
{
    ValueTask SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        CancellationToken cancellationToken = default) where TData : class;
}