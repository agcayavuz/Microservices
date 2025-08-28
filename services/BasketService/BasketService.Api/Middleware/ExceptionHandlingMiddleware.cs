using BasketService.Api.Common;
using System.Net;
using System.Text.Json;

namespace BasketService.Api.Middleware
{

    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            // 🔐 Dağıtık kilit alınamadı → 429 (veya 503)
            catch (InvalidOperationException ex) when (ex.Message == "basket_locked")
            {
                await WriteError(
                    context,
                    HttpStatusCode.TooManyRequests,               // istersen: HttpStatusCode.ServiceUnavailable
                    code: "busy",
                    message: "Basket is busy, please retry.",
                    details: null);
            }
            // (Opsiyonel) İstek iptal oldu → genelde client kapattı; 400 serisi döndürüp logla
            //catch (OperationCanceledException)
            //{
            //    await WriteError(
            //        context,
            //        HttpStatusCode.BadRequest,
            //        code: "request_canceled",
            //        message: "Request was canceled by the client.",
            //        details: null);
            //}
            catch (ArgumentException ex)
            {
                await WriteError(context,
                    HttpStatusCode.BadRequest,
                    code: "validation_error",
                    message: ex.Message,
                    details: ex.ParamName);
            }
            catch (Exception ex)
            {
                await WriteError(context,
                    HttpStatusCode.InternalServerError,
                    code: "unhandled_exception",
                    message: ex.Message,
                    details: null);
            }
        }

        private static async Task WriteError(HttpContext ctx, HttpStatusCode status, string code, string message, object? details)
        {
            // Response metadata
            ctx.Response.StatusCode = (int)status;
            ctx.Response.ContentType = "application/json";

            // CorrelationId varsa onu kullan (yoksa TraceIdentifier)
            var traceId =
                ctx.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var corr)
                    ? corr.ToString()
                    : ctx.TraceIdentifier;

            var payload = ApiResponse<object>.Fail(code, message, details, traceId);
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }
}
