using Xcel.Config.Options;

namespace Xcel.Config;

public interface IOptionsValidator
{
    void Validate(EnvironmentOptions environmentOptions);
}
