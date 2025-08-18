namespace CatalogService.Api.Middlewares
{
    public sealed class CorrelationIdMiddleware(RequestDelegate next)
    {
        private const string HeaderName = "X-Correlation-Id";

        public async Task Invoke(HttpContext context)
        {
            // Request’te varsa al, yoksa üret
            var cid = context.Request.Headers.TryGetValue(HeaderName, out var reqCid)
                ? reqCid.ToString()
                : Guid.NewGuid().ToString("N");

            // Response header’a yaz (overwrite etmekten çekinme)
            context.Response.Headers[HeaderName] = cid;

            // Downstream erişim için Items’a koy
            context.Items[HeaderName] = cid;

            await next(context);
        }
    }
}
