using Domain.Results;
using Xcel.Services.Email.Models;

namespace Xcel.Services.Email.Interfaces;

public interface IEmailSender
{
    Task<Result> SendEmailAsync<TData>(
        EmailPayload<TData> payload,
        CancellationToken cancellationToken = default) where TData : class;
}