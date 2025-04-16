using Xcel.Config;
using Xcel.Config.Options;

namespace Xcel.Services.Email;

public class EmailOptions : IOptionsValidator
{
    public required string ApiKey { get; set; }

    public required string BaseUrl { get; set; }

    public void Validate(EnvironmentOptions environmentOptions)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new ArgumentException("BaseUrl cannot be null or whitespace.", nameof(BaseUrl));
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ArgumentException("ApiKey cannot be null or whitespace.", nameof(ApiKey));
        }
    }
}