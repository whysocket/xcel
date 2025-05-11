using Infra;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Presentation.API;

public class EmailServiceHealthCheck(HttpClient httpClient, InfraOptions infraOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Retrieve the base URL for the email service from configuration
            // The key InfraOptions:Email:BaseUrl is based on how you structured your options
            var emailServiceBaseUrl = infraOptions.Email.BaseUrl;

            if (string.IsNullOrEmpty(emailServiceBaseUrl))
            {
                return HealthCheckResult.Unhealthy("Email service base URL is not configured.");
            }

            // Make a simple GET request to the email service's /health endpoint
            // We use a try-catch here as HttpClient.GetAsync can throw exceptions for connection issues, timeouts, etc.
            var response = await httpClient.GetAsync("/health", cancellationToken);

            // Check if the response status code indicates success (2xx)
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Email service is reachable and healthy.");
            }
         
            return HealthCheckResult.Unhealthy($"Email service returned status code {response.StatusCode}.");
        }
        catch (HttpRequestException httpEx)
        {
            // Catch specific HTTP request exceptions (network issues, DNS problems, etc.)
            return HealthCheckResult.Unhealthy($"Email service HTTP request failed: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            // Catch any other exceptions during the health check
            return HealthCheckResult.Unhealthy($"Email service health check failed: {ex.Message}");
        }
    }
}