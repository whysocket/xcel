using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Services.Xcel.Auth;

internal sealed class HttpClientInfoService(IHttpContextAccessor httpContextAccessor) : IClientInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public string GetIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new NullReferenceException("HttpContext is null");
        }

        string? ipAddress = null;

        // Try X-Forwarded-For header (comma-separated list, first is client IP)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            ipAddress = forwardedFor.Split(',')[0].Trim();
        }

        // If X-Forwarded-For is not available, try Cf-Connecting-Ip (Cloudflare)
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Request.Headers["Cf-Connecting-Ip"].FirstOrDefault();
        }

        // If both X-Forwarded-For and Cf-Connecting-Ip are not available, fall back to RemoteIpAddress
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        // If all fail, return "Unknown"
        return ipAddress ?? "Unknown";
    }
}