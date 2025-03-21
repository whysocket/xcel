using Infra.Options;
using Xcel.Config;
using Xcel.Config.Options;
using Xcel.Services.Auth;
using Xcel.Services.Email;

namespace Infra;

public class InfraOptions : IOptionsValidator
{
    public required DatabaseOptions Database { get; set; }
    public required EmailOptions Email { get; set; }
    public required AuthOptions Auth { get; set; }

    public void Validate(EnvironmentOptions environmentOptions)
    {
        Database.Validate(environmentOptions);
        Email.Validate(environmentOptions);
        Auth.Validate(environmentOptions);
    }
}