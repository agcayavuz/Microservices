using System.Diagnostics;

namespace BasketService.Api.Middleware
{

    public sealed class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        public const string HeaderName = "X-Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var h) && !string.IsNullOrWhiteSpace(h)
                ? h.ToString()
                : Guid.NewGuid().ToString("n");

            context.TraceIdentifier = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using var _ = new Activity("request").Start();
            Activity.Current?.AddTag("correlation_id", correlationId);

            await _next(context);
        }
    }
}
