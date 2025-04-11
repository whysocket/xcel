using Xcel.Config.Options;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Webhooks.Strategies.Discord;

public enum DiscordColors
{
    Red = 16711680,   // #FF0000
    Green = 65280,     // #00FF00
    Blue = 255,        // #0000FF
    Pink = 16711935,   // #FF69B3
    Yellow = 16776960, // #FFFF00
    Orange = 16753920, // #FFA500
    Purple = 8388736,  // #800080
    Cyan = 65535,      // #00FFFF
    White = 16777215,  // #FFFFFF
    Black = 0          // #000000
}

internal class DiscordPayloadBuilder(
    TimeProvider timeProvider,
    IClientInfoService httpClientInfoService,
    EnvironmentOptions environmentOptions)
{
    public object BuildPayload(Exception exception, HttpContext httpContext)
    {
        var embeds = new[]
        {
            CreateExceptionEmbed(exception),
            CreateMetadataEmbed(httpContext),
            CreateDeveloperEmbed(exception, httpContext)
        };

        return new
        {
            username = "Exception Bot",
            avatar_url = "https://i.imgur.com/4M34hi2.png",
            embeds
        };
    }

    private object CreateExceptionEmbed(Exception exception)
    {
        var time = timeProvider.GetUtcNow();
        var unixTimestamp = new DateTimeOffset(time.UtcDateTime).ToUnixTimeSeconds();

        return new
        {
            title = $"[{environmentOptions.Type}] - {exception.GetType().Name}",
            color = DiscordColors.Red,
            fields = new[]
            {
                new { name = "Type", value = exception.GetType().Name, inline = false },
                new { name = "Message", value = exception.Message, inline = false },
                new { name = "Date", value = $"{time:dd/MM}", inline = true },
                new { name = "Time", value = $"{time:HH:mm:ss}", inline = true },
                new { name = "Weekday", value = $"{time.DayOfWeek}", inline = true },
                new { name = "Unix Timestamp", value = $"{unixTimestamp}", inline = true },
            },
        };
    }

    private object CreateMetadataEmbed(HttpContext httpContext)
    {
        return new
        {
            title = "Metadata",
            color = DiscordColors.Pink,
            fields = new[]
            {
                new { name = "User Agent", value = httpContext.Request.Headers.UserAgent.ToString(), inline = false },
                new { name = "Method", value = httpContext.Request.Method, inline = true },
                new { name = "Request Path", value = httpContext.Request.Path.Value ?? string.Empty, inline = true },
                new { name = "IP Address", value = httpClientInfoService.GetIpAddress(), inline = true }
            }
        };
    }

    private object CreateDeveloperEmbed(Exception exception, HttpContext httpContext)
    {
        var headers = httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        var formattedHeaders = headers.Select(header =>
        {
            var key = header.Key;
            var value = header.Value.Length > 50 ? header.Value.Substring(0, 50) + "..." : header.Value; // Truncate long values
            return $"{key}: {value}";
        });

        var headersString = string.Join("\n", formattedHeaders);

        var stackTrace = exception.StackTrace;
        if (!string.IsNullOrEmpty(stackTrace))
        {
            stackTrace = FormatStackTrace(stackTrace);
        }

        return new
        {
            color = (int)DiscordColors.White,
            author = new { name = "For Developers" },
            fields = new[]
            {
                new
                {
                    name = "Request Headers",
                    value = $"```{headersString.Substring(0, Math.Min(1024, headersString.Length))}```",
                    inline = false
                },
                new
                {
                    name = "Stack Trace",
                    value = $"```{stackTrace?.Substring(0, Math.Min(500, stackTrace.Length)).Trim()}```",
                    inline = false
                }
            },
            timestamp = timeProvider.GetUtcNow(),
        };
    }
    private static string FormatStackTrace(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return stackTrace;
        }

        var lines = stackTrace.Split('\n');
        var formattedLines = lines.Select(FormatStackTraceLine);
        return string.Join("\n ", formattedLines);
    }

    private static string FormatStackTraceLine(string line)
    {
        var trimmedLine = line.TrimStart();

        if (trimmedLine.StartsWith("at "))
        {
            return trimmedLine.Substring(3).TrimStart();
        }

        return trimmedLine;
    }
}