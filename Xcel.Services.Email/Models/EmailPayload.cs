namespace Xcel.Services.Email.Models;

public record EmailPayload<TData>(
    string Subject,
    string To,
    TData Data) where TData : class
{
    private string body = null!;

    public string Body
    {
        get => body;
        set
        {
            if (!string.IsNullOrEmpty(body))
            {
                throw new InvalidOperationException("You can only set the body with value once.");
            }

            body = value;
        }
    }

}