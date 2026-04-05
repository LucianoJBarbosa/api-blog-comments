using Microsoft.Extensions.Diagnostics.HealthChecks;
using api_blog_comments_dev.Data;

namespace api_blog_comments_dev.Infrastructure;

public sealed class DatabaseHealthCheck(IDbConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            return connection.State == System.Data.ConnectionState.Open
                ? HealthCheckResult.Healthy("Database connection opened successfully.")
                : HealthCheckResult.Unhealthy("Database connection could not be opened.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", exception);
        }
    }
}