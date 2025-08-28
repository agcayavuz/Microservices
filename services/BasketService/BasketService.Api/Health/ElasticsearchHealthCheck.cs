using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace BasketService.Api.Health
{

    public sealed class ElasticsearchHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ElasticsearchHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var esUri = _config.GetValue<string>("Serilog:Elasticsearch:Uri");
            if (string.IsNullOrWhiteSpace(esUri))
                return HealthCheckResult.Unhealthy("Serilog:Elasticsearch:Uri is missing.");

            try
            {
                var client = _httpClientFactory.CreateClient(nameof(ElasticsearchHealthCheck));
                // BaseAddress Program.cs’de ayarlanmış olacak. Kök path “/”’e GET atalım:
                var resp = await client.GetAsync("/", cancellationToken);
                if (!resp.IsSuccessStatusCode)
                    return HealthCheckResult.Unhealthy($"Elasticsearch returned {(int)resp.StatusCode}");

                // Güvenli JSON parse (version.number almaya çalış, olmazsa Healthy mesajı kısa kalsın)
                var content = await resp.Content.ReadAsStringAsync(cancellationToken);
                string? version = null;
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("version", out var verObj) &&
                        verObj.TryGetProperty("number", out var numEl) &&
                        numEl.ValueKind == JsonValueKind.String)
                    {
                        version = numEl.GetString();
                    }
                }
                catch
                {
                    // ES yanıtı beklenmedik formatta ise versiyonu yoksayalım
                }

                return HealthCheckResult.Healthy(version is null
                    ? "Elasticsearch OK"
                    : $"Elasticsearch OK (version={version})");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Elasticsearch unreachable", ex);
            }
        }
    }

}
