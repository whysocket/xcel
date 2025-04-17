using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Models;

public record EmailPayload<TData>(
    IEnumerable<string> To,
    TData Data) where TData : IEmail
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


    public string Subject => Data.Subject;
    
    public EmailPayload(string to, TData data) : this([to], data)
    {
    }
}