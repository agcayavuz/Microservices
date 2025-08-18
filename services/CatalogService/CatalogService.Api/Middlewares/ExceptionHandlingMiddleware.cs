using CatalogService.Api.Responses;
using System.Net;
using System.Text.Json;

namespace CatalogService.Api.Middlewares
{
    public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await WriteProblem(context, "Operation was canceled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteProblem(context, "Unexpected error occurred.");
            }
        }

        private static Task WriteProblem(HttpContext ctx, string message)
        {
            ctx.Response.ContentType = "application/json";
            var correlationId = ctx.Response.Headers.TryGetValue("X-Correlation-Id", out var cid)
                ? cid.ToString()
                : null;

            var payload = JsonSerializer.Serialize(ApiResponse<object>.Fail(message, correlationId));
            return ctx.Response.WriteAsync(payload);
        }
    }
}
