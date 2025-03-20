using Microsoft.AspNetCore.Diagnostics;
using Presentation.API.Extensions;
using Presentation.API.Webhooks;
using Presentation.API.Webhooks.Enums;
using Presentation.API.Webhooks.Strategies.Discord;

namespace Presentation.API;

public class GlobalExceptionHandler(
    DiscordPayloadBuilder discordPayloadBuilder,
    WebhookSenderManager webhookSenderManager,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = ResultExtensions.CreateProblemDetailsFromDomainResult(exception.MapToResult(), httpContext);

        httpContext.Response.StatusCode = (int)problemDetails.Status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

        await SendDiscordNotification(exception, httpContext);
        logger.LogError(exception, "[GlobalExceptionHandler] Exception handled: {Message}", exception.Message);

        return true;
    }

    private async Task SendDiscordNotification(Exception exception, HttpContext httpContext)
    {
        var payload = discordPayloadBuilder.BuildPayload(exception, httpContext);
        await webhookSenderManager.SendWebhookAsync(WebhookType.Discord, payload);
    }
}