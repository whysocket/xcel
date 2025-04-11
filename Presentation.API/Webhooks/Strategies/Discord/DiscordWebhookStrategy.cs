using System.Net.Mime;
using System.Text.Json;
using Presentation.API.Options;
using Presentation.API.Webhooks.Interfaces;

namespace Presentation.API.Webhooks.Strategies.Discord;

internal sealed class DiscordWebhookStrategy(
    IHttpClientFactory httpClientFactory,
    ApiOptions apiOptions,
    ILogger<DiscordWebhookStrategy> logger)
    : IWebhookStrategy
{
    public async Task SendWebhookAsync(object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, MediaTypeNames.Application.Json);

            using var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(apiOptions.Webhooks.DiscordUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                logger.LogError($"[DiscordWebhookStrategy] Error sending Discord notification. Status: {response.StatusCode}, Content: {responseContent}");
            }
            else
            {
                logger.LogInformation("[DiscordWebhookStrategy] Discord notification sent successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DiscordWebhookStrategy] Error sending Discord notification: {Message}", ex.Message);
        }
    }
}