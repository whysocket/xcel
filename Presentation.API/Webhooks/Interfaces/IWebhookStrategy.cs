namespace Presentation.API.Webhooks.Interfaces;

public interface IWebhookStrategy
{
    Task SendWebhookAsync(object payload);
}
