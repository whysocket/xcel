namespace Xcel.Services.Models;

public record EmailPayload<TData>(
    string Subject,
    string To,
    TData Data) where TData : class;
