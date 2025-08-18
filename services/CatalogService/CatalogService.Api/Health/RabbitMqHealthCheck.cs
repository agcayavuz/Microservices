using CatalogService.Infrastructure.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CatalogService.Api.Health
{
    public sealed class RabbitMqHealthCheck(RabbitMqConnection conn) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            try
            {
                using var ch = conn.CreateChannel();
                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ channel open failed", ex));
            }
        }
    }
}
