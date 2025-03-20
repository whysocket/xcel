using Application;
using Application.Config;
using Xcel.Services.Email;

namespace Infra.Options;

public class DatabaseOptions
{
    public required string ConnectionString { get; set; }
    public DevPowersOptions? DevPowers { get; set; }

    public void Validate(EnvironmentConfig environment)
    {
        if (!environment.IsDevelopment() && DevPowers != null)
        {
            throw new ArgumentException("DevPowers must be null outside of the Development environment.");
        }
    }
}

public class DevPowersOptions
{
    public DatabaseDevPower Recreate { get; set; } = DatabaseDevPower.None;
    public DatabaseDevPower Migrate { get; set; } = DatabaseDevPower.None;
}

public enum DatabaseDevPower
{
    None,
    Always
}

public class InfraOptions
{
    public required DatabaseOptions Database { get; set; }
    public required EmailOptions Email { get; set; }
}
