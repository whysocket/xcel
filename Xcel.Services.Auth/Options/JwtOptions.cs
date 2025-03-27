using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xcel.Config;
using Xcel.Config.Options;

namespace Xcel.Services.Auth.Options;

public class JwtOptions : IOptionsValidator
{
    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public required string SecretKey { get; set; }

    public required int ExpireInMinutes { get; set; }

    public byte[] SecretKeyEncoded => Encoding.UTF8.GetBytes(SecretKey);

    public TokenValidationParameters TokenValidationParameters => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(SecretKeyEncoded),
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ClockSkew = TimeSpan.Zero,
    };

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

        if (SecretKey.Length < 32)
        {
            throw new ArgumentException("SecretKey must be at least 32 characters long.", nameof(SecretKey));
        }

        const int oneDayInMinutes = 24 * 60;
        if (ExpireInMinutes is <= 0 or > oneDayInMinutes)
        {
            throw new ArgumentException($"ExpireInMinutes must be a positive value, and less than {oneDayInMinutes} minutes (24 Hours).", nameof(ExpireInMinutes));
        }
    }
}