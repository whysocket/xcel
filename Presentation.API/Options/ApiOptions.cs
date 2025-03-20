namespace Presentation.API.Options;

public class ApiOptions
{
    public required Webhooks Webhooks { get; set; } = null!;
}

public class Webhooks
{
    public required string DiscordUrl { get; set; } = null!;
}