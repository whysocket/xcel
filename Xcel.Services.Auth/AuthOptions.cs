using Xcel.Config;
using Xcel.Config.Options;
using Xcel.Services.Auth.Options;

namespace Xcel.Services.Auth;

public class AuthOptions : IOptionsValidator
{
    public required JwtOptions Jwt { get; set; }

    public void Validate(EnvironmentOptions environmentOptions)
    {
        Jwt.Validate(environmentOptions);
    }
}