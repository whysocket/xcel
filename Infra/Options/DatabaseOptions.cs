using Xcel.Config;
using Xcel.Config.Options;

namespace Infra.Options;

public class DatabaseOptions : IOptionsValidator
{
    public required string ConnectionString { get; set; }
    public void Validate(EnvironmentOptions environment)
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new ArgumentException("[DatabaseOptions] Connection string is empty");
        }
    }
}
