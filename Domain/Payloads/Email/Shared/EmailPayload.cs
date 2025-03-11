namespace Domain.Payloads.Email.Shared;

public record EmailPayload<TData>(
    string Subject,
    string To,
    TData Data) where TData : class;
