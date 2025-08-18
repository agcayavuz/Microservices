using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CatalogService.Api.Health
{
    public sealed class ElasticsearchHealthCheck(IConfiguration cfg) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            var uri = cfg.GetValue<string>("Serilog:Elasticsearch:Uri") ?? "http://localhost:9200";
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var res = await http.GetAsync(uri, ct);
                if (!res.IsSuccessStatusCode)
                    return HealthCheckResult.Degraded($"Elasticsearch responded {(int)res.StatusCode}");
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Elasticsearch unreachable", ex);
            }
        }
    }
}
