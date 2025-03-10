namespace Domain.Payloads.Email.Shared;

public record EmailPayload<T>(
    string To,
    string Subject,
    T TemplateData) where T : ITemplateData;

public interface ITemplateData
{
}