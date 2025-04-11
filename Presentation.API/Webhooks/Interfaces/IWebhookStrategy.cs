using System.Net.Mime;
using System.Text.Json;

namespace Presentation.API.Webhooks.Interfaces;

public interface IWebhookStrategy
{
    Task SendWebhookAsync(object payload);
}
