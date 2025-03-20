using Presentation.API.Webhooks.Abstractions;
using Presentation.API.Webhooks.Enums;
using Presentation.API.Webhooks.Strategies.Discord;

namespace Presentation.API.Webhooks;

public class WebhookSenderManager(
    IServiceProvider serviceProvider,
    ILogger<WebhookSenderManager> logger)
{
    private readonly Dictionary<WebhookType, Type> _strategyTypes = new()
    {
        { WebhookType.Discord, typeof(DiscordWebhookStrategy) },
    };

    public async Task SendWebhookAsync(WebhookType webhookType, object payload)
    {
        if (_strategyTypes.TryGetValue(webhookType, out var strategyType))
        {
            try
            {
                if (serviceProvider.GetService(strategyType) is IWebhookStrategy strategy)
                {
                    await strategy.SendWebhookAsync(payload);
                    return;
                }

                logger.LogError($"[WebhookSenderManager] Could not resolve webhook strategy for type: {strategyType.FullName}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[WebhookSenderManager] Error sending webhook for type: {strategyType.FullName}");
            }
        }

        logger.LogError($"[WebhookSenderManager] Webhook type '{webhookType}' not found.");
    }
}