using Domain.Payloads.Email.Shared;

namespace Domain.Interfaces.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync<T>(EmailPayload<T> payload) where T : ITemplateData;
}