using System.Text;
using Xcel.Config;
using Xcel.Config.Options;

namespace Xcel.Services.Auth.Options;

public class JwtOptions : IOptionsValidator
{
    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public required string SecretKey { get; set; }

    public byte[] SecretKeyEncoded => Encoding.ASCII.GetBytes(SecretKey);

    public void Validate(EnvironmentOptions environmentOptions)
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new ArgumentException("Issuer cannot be null or whitespace.", nameof(Issuer));
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new ArgumentException("Audience cannot be null or whitespace.", nameof(Audience));
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new ArgumentException("SecretKey cannot be null or whitespace.", nameof(SecretKey));
        }

        if (SecretKey.Length < 16)
        {
            throw new ArgumentException("SecretKey must be at least 16 characters long.", nameof(SecretKey));
        }
    }
}