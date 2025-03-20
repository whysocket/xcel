using Presentation.API.Webhooks.Strategies.Discord;

namespace Presentation.API.Webhooks;

public static class DependencyInjection
{
    public static IServiceCollection AddWebhooks(this IServiceCollection services)
    {
        return services
            .AddSingleton<DiscordPayloadBuilder>()
            .AddSingleton<WebhookSenderManager>()
            .AddSingleton<DiscordWebhookStrategy>();
    }
}