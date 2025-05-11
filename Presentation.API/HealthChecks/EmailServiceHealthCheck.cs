using Infra;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Presentation.API.HealthChecks;

public class EmailServiceHealthCheck(HttpClient httpClient, InfraOptions infraOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var emailServiceBaseUrl = infraOptions.Email.BaseUrl;
        var healthCheckUrl = emailServiceBaseUrl?.TrimEnd('/') + "/health";

        var healthData = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(healthCheckUrl))
        {
             healthData["checkedUrl"] = healthCheckUrl;
        }

        if (string.IsNullOrEmpty(emailServiceBaseUrl))
        {
            healthData["reason"] = "Email service base URL is not configured.";
            return HealthCheckResult.Unhealthy("Email service base URL is not configured.", data: healthData);
        }

        try
        {
            var response = await httpClient.GetAsync(healthCheckUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                healthData["statusCode"] = (int)response.StatusCode;
                return HealthCheckResult.Healthy("Email service is reachable and healthy.", data: healthData);
            }

            healthData["statusCode"] = (int)response.StatusCode;
            return HealthCheckResult.Unhealthy($"Email service returned status code {response.StatusCode}.", data: healthData);
        }
        catch (HttpRequestException httpEx)
        {
            healthData["errorType"] = "HttpRequestException";
            healthData["errorMessage"] = httpEx.Message;
            return HealthCheckResult.Unhealthy($"Email service HTTP request failed: {httpEx.Message}", exception: httpEx, data: healthData);
        }
        catch (Exception ex)
        {
            healthData["errorType"] = "Exception";
            healthData["errorMessage"] = ex.Message;
            return HealthCheckResult.Unhealthy($"Email service health check failed: {ex.Message}", exception: ex, data: healthData);
        }
    }
}