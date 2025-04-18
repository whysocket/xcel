using Xcel.Config;
using Xcel.Config.Options;

namespace Infra.Options;

public class DevPowersOptions : IOptionsValidator
{
    public bool Recreate { get; set; } = false;
    public DatabaseDevPower Migrate { get; set; } = DatabaseDevPower.None;
    public bool Seed { get; set; } = false;

    public void Validate(EnvironmentOptions environmentOptions)
    {
        if (environmentOptions.IsProduction())
        {
            if (Recreate)
            {
                throw new ArgumentException("[DevPowersOptions] Database recreation is not supported in production.");
            }

            if (Migrate != DatabaseDevPower.None)
            {
                throw new ArgumentException("[DevPowersOptions] Database migration is not supported in production.");
            }

            if (Seed)
            {
                throw new ArgumentException("[DevPowersOptions] Database seeding is not supported in production.");
            }
        }
    }
}