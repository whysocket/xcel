using System.Security.Claims;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Services.Xcel.Auth;

internal sealed class HttpClientInfoService(IHttpContextAccessor httpContextAccessor)
    : IClientInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public string IpAddress
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                throw new NullReferenceException("HttpContext is null");
            }

            var ipAddress = httpContext.Request.Headers["Cf-Connecting-Ip"].FirstOrDefault();

            if (string.IsNullOrEmpty(ipAddress))
            {
                var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    ipAddress = forwardedFor.Split(',')[0].Trim();
                }
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? "Unknown";
        }
    }

    public Guid UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                throw new NullReferenceException("HttpContext is null");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException("User ID (uid) not found in the current context.");
        }
    }
}
