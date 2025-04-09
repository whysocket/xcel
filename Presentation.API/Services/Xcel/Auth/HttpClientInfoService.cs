using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Services.Xcel.Auth;

public class HttpClientInfoService(IHttpContextAccessor httpContextAccessor) : IClientInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public string GetIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return "Unknown"; // Or throw an exception, depending on your needs
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

        return ipAddress ?? "Unknown"; // Handle null IP address
    }
}