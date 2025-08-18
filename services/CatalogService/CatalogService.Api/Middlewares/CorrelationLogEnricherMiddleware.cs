using Serilog.Context;

namespace CatalogService.Api.Middlewares
{
    public sealed class CorrelationLogEnricherMiddleware(RequestDelegate next)
    {
        public async Task Invoke(HttpContext context)
        {
            const string header = "X-Correlation-Id";
            var cid = context.Response.Headers[header].ToString();
            // Response header’a yazmadan önce üretildiği için null olabilir; gerekirse Request’ten al
            if (string.IsNullOrWhiteSpace(cid) && context.Request.Headers.TryGetValue(header, out var reqCid))
                cid = reqCid.ToString();

            using (LogContext.PushProperty("CorrelationId", string.IsNullOrWhiteSpace(cid) ? null : cid))
            using (LogContext.PushProperty("ServiceName", "CatalogService"))
            using (LogContext.PushProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
            {
                await next(context);
            }
        }
    }
}
