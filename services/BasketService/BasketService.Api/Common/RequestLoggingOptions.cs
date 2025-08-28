namespace BasketService.Api.Common
{
    public sealed class RequestLoggingOptions
    {
        public bool Enable { get; set; } = true;

        // Path ve header filtreleri
        public bool IncludeQueryInRequestPath { get; set; } = true;
        public string[] ExcludePaths { get; set; } = new[] { "/health", "/health/live", "/health/ready", "/swagger" };
        public string[] CaptureHeaders { get; set; } = new[] { "X-Correlation-Id", "User-Agent", "X-Forwarded-For" };
        public string[] ExcludeHeaders { get; set; } = new[] { "Authorization", "Cookie", "Set-Cookie" };

        // Gövde loglama (isteğe bağlı – prod’da kapalı)
        public bool LogRequestBody { get; set; } = false;
        public int MaxRequestBodyBytes { get; set; } = 4096;

        public bool LogResponseBody { get; set; } = false;
        public int MaxResponseBodyBytes { get; set; } = 4096;
    }
}
