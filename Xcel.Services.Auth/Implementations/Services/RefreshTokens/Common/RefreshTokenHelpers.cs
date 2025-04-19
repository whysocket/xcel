using System.Security.Cryptography;

namespace Xcel.Services.Auth.Implementations.Services.RefreshTokens.Common;

internal static class RefreshTokenHelpers
{
    internal static string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}