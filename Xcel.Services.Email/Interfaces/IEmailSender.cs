using Xcel.Services.Models;

namespace Xcel.Services.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        string body,
        CancellationToken cancellationToken = default) where TData : class;
}