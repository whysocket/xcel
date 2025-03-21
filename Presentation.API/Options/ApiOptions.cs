using Xcel.Config;
using Xcel.Config.Options;

namespace Presentation.API.Options;

public class ApiOptions : IOptionsValidator
{
    public required Webhooks Webhooks { get; set; } = null!;

    public void Validate(EnvironmentOptions environmentOptions)
    {
        Webhooks.Validate(environmentOptions);
    }
}