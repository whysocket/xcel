namespace Xcel.Services.Email.Models;

public record EmailPayload<TData>(
    string Subject,
    string To,
    TData Data) where TData : class
{
    private string _body = null!;

    public string Body
    {
        get => _body;
        set
        {
            if (!string.IsNullOrEmpty(_body))
            {
                throw new InvalidOperationException("You can only set the body with value once.");
            }

            _body = value;
        }
    }

}