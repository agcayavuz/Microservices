using Serilog.Context;

namespace BasketService.Api.Middleware
{

    public sealed class CorrelationLogEnricherMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationLogEnricherMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
            using (LogContext.PushProperty("correlation_id", correlationId))
            {
                await _next(context);
            }
        }
    }
}
