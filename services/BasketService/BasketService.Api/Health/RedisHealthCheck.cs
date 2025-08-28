using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace BasketService.Api.Health
{
    public sealed class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _mux;
        public RedisHealthCheck(IConnectionMultiplexer mux) => _mux = mux;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var ping = await _mux.GetDatabase().PingAsync();
                return HealthCheckResult.Healthy($"Redis OK (ping={ping.TotalMilliseconds:0.##} ms)");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Redis unreachable", ex);
            }
        }
    }
}
