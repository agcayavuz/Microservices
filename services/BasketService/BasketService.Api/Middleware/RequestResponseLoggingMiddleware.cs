using System.Text;
using BasketService.Api.Common;
using Microsoft.Extensions.Options;
using Serilog;

namespace BasketService.Api.Middleware
{

    public sealed class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptionsMonitor<RequestLoggingOptions> _opt;

        public RequestResponseLoggingMiddleware(RequestDelegate next, IOptionsMonitor<RequestLoggingOptions> opt)
        {
            _next = next;
            _opt = opt;
        }

        public async Task Invoke(HttpContext context)
        {
            var o = _opt.CurrentValue;
            if (!o.Enable || IsExcludedPath(context, o)) { await _next(context); return; }

            // Header’ları al (exclude listesi hariç)
            var headers = context.Request.Headers
                .Where(h => !o.ExcludeHeaders.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
                .Where(h => o.CaptureHeaders.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => (string)h.Value);

            // ---- Request body
            string? requestBody = null;
            if (o.LogRequestBody && context.Request.ContentLength is > 0 && context.Request.Body.CanRead)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var buf = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                requestBody = Truncate(buf, o.MaxRequestBodyBytes);
            }

            var originalBody = context.Response.Body;
            await using var mem = new MemoryStream();
            context.Response.Body = mem;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();

                // ---- Response body
                string? responseBody = null;
                mem.Position = 0;
                if (o.LogResponseBody)
                {
                    using var r = new StreamReader(mem, Encoding.UTF8, leaveOpen: true);
                    var buf = await r.ReadToEndAsync();
                    responseBody = Truncate(buf, o.MaxResponseBodyBytes);
                    mem.Position = 0;
                }

                // Log (ECS ile uyumlu alan isimleri)
                Log.Information("HTTP {Method} {Path} -> {StatusCode} in {Elapsed} ms",
                    context.Request.Method,
                    o.IncludeQueryInRequestPath ? context.Request.Path + context.Request.QueryString : context.Request.Path.ToString(),
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds);

                Log.Debug("req: headers={Headers} body={RequestBody}", headers, requestBody);
                if (o.LogResponseBody)
                    Log.Debug("res: status={StatusCode} body={ResponseBody}", context.Response.StatusCode, responseBody);

                // Response’u geri yaz
                await mem.CopyToAsync(originalBody);
                context.Response.Body = originalBody;
            }
        }

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max) + "...(truncated)";

        private static bool IsExcludedPath(HttpContext ctx, RequestLoggingOptions o)
            => o.ExcludePaths.Any(p => ctx.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}
