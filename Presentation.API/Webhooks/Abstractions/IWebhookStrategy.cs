using System.Net.Mime;
using System.Text.Json;

namespace Presentation.API.Webhooks.Abstractions;

public interface IWebhookStrategy
{
    Task SendWebhookAsync(object payload);
}

public class SlackWebhookStrategy(
    IHttpClientFactory httpClientFactory) : IWebhookStrategy
{
    public async Task SendWebhookAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, MediaTypeNames.Application.Json);

        var client = httpClientFactory.CreateClient();
        await client.PostAsync("https://webhook.site/82d27119-1dcc-44a5-9243-9755d4d7d39d", content);;
    }
}