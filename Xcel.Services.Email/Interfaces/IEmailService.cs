using Xcel.Services.Models;

namespace Xcel.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync<TData>(EmailPayload<TData> payload, CancellationToken cancellationToken = default)
        where TData : class;
}