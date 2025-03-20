namespace Presentation.API.Webhooks.Abstractions;

public interface IWebhookStrategy
{
    Task SendWebhookAsync(object payload);
}