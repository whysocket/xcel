using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Presentation.API;

public class EmailServiceHealthCheck(HttpClient httpClient, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Retrieve the base URL for the email service from configuration
            // The key InfraOptions:Email:BaseUrl is based on how you structured your options
            var emailServiceBaseUrl = configuration["Infra:Email:BaseUrl"];

            if (string.IsNullOrEmpty(emailServiceBaseUrl))
            {
                return HealthCheckResult.Unhealthy("Email service base URL is not configured.");
            }

            // Construct a health check URL. You might need to adjust this
            // if your email service has a specific health check endpoint.
            // A simple GET to the base URL might suffice, or it could be something like "/status" or "/health".
            // For this example, we'll just use the base URL.
            var healthCheckUrl = emailServiceBaseUrl.TrimEnd('/') + "/"; // Ensure a trailing slash for base URL check

            // Make a simple GET request to the email service
            // We use a try-catch here as HttpClient.GetAsync can throw exceptions for connection issues, timeouts, etc.
            var response = await httpClient.GetAsync(healthCheckUrl, cancellationToken);

            // Check if the response status code indicates success (2xx)
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Email service is reachable.");
            }
            else
            {
                // If the status code is not successful, report as unhealthy with details
                return HealthCheckResult.Unhealthy($"Email service returned status code {response.StatusCode}.");
            }
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