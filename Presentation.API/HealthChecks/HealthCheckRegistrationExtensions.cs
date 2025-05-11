using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Presentation.API.HealthChecks;

public static class HealthCheckRegistrationExtensions
{
    /// <summary>
    /// Adds the Email Service health check to the health checks builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="infraOptions">The infrastructure options containing the Email BaseUrl.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddEmailServiceCheck(
        this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<EmailServiceHealthCheck>(
            "EmailService",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["email", "external"],
            timeout: TimeSpan.FromSeconds(5)
        );
    }

    /// <summary>
    /// Adds the Entity Framework Core Database health check for a specific DbContext.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext to check.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddDatabaseCheck<TDbContext>(
        this IHealthChecksBuilder builder)
        where TDbContext : DbContext
    {
        return builder.AddDbContextCheck<TDbContext>(
            name: "Database",
            failureStatus: HealthStatus.Degraded, 
            tags: ["db", "internal"]
        );
    }
}