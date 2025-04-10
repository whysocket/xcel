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

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

        return ipAddress ?? "Unknown";
    }
}