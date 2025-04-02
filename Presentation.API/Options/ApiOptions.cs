using Xcel.Config;
using Xcel.Config.Options;

namespace Presentation.API.Options;

public class ApiOptions : IOptionsValidator
{
    public required WebhooksOptions Webhooks { get; set; } = null!;

    public required CorsOptions Cors { get; set; } = null!;

    public void Validate(EnvironmentOptions environmentOptions)
    {
        Webhooks.Validate(environmentOptions);
        Cors.Validate(environmentOptions);
    }
}