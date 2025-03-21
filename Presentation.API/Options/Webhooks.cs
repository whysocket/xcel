using Xcel.Config;
using Xcel.Config.Options;

namespace Presentation.API.Options;

public class Webhooks : IOptionsValidator
{
    public required string DiscordUrl { get; set; } = null!;

    public void Validate(EnvironmentOptions environmentOptions)
    {
        if (string.IsNullOrWhiteSpace(DiscordUrl))
        {
            throw new ArgumentException("Discord url is required.", nameof(DiscordUrl));
        }

        try
        {
            var isValidScheme = false;
            var result = Uri.TryCreate(DiscordUrl, UriKind.Absolute, out var uriResult);
            if (result)
            {
                isValidScheme = uriResult?.Scheme == Uri.UriSchemeHttp || uriResult?.Scheme == Uri.UriSchemeHttps;
            }

            if (!result || !isValidScheme)
            {
                throw new ArgumentException("Discord url is not a valid URL.", nameof(DiscordUrl));
            }
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Discord url is not a valid URL.", nameof(DiscordUrl));
        }
    }
}