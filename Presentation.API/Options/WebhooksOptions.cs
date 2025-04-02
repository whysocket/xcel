using Xcel.Config;
using Xcel.Config.Options;

namespace Presentation.API.Options;

public class WebhooksOptions : IOptionsValidator
{
    public required string DiscordUrl { get; set; }

    public void Validate(EnvironmentOptions environment)
    {
        if (string.IsNullOrWhiteSpace(DiscordUrl))
        {
            throw new ArgumentException("[WebhooksOptions] DiscordUrl is required.", nameof(DiscordUrl));
        }

        if (!Uri.TryCreate(DiscordUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("[WebhooksOptions] DiscordUrl is not a valid absolute URL.", nameof(DiscordUrl));
        }
    }
}