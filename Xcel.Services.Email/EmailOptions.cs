using Xcel.Config;
using Xcel.Config.Options;

namespace Xcel.Services.Email;

public class EmailOptions : IOptionsValidator
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromAddress { get; set; }
    public required bool EnableSsl { get; set; }

    public void Validate(EnvironmentOptions environmentOptions)
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new ArgumentException("Email host cannot be null or whitespace.", nameof(Host));
        }

        if (Port <= 0 || Port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), "Email port must be within the valid port range (1-65535).");
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            throw new ArgumentException("Email username cannot be null or whitespace.", nameof(Username));
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new ArgumentException("Email password cannot be null or whitespace.", nameof(Password));
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            throw new ArgumentException("Email from address cannot be null or whitespace.", nameof(FromAddress));
        }
    }
}