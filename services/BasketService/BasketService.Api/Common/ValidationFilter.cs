using FluentValidation;
using FluentValidation.Results;

namespace BasketService.Api.Common
{
    public sealed class ValidationFilter<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
        {
            var validator = ctx.HttpContext.RequestServices.GetService<IValidator<T>>();
            var model = ctx.Arguments.OfType<T>().FirstOrDefault();

            if (validator is null || model is null)
                return await next(ctx);

            ValidationResult result = await validator.ValidateAsync(model, ctx.HttpContext.RequestAborted);
            if (result.IsValid) return await next(ctx);

            var errors = result.Errors.GroupBy(e => e.PropertyName)
                                      .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var cid = ctx.HttpContext.Response.Headers[Middleware.CorrelationIdMiddleware.HeaderName].ToString();
            return Results.BadRequest(ApiResponse<object>.Fail("validation_error", "Request validation failed", errors, cid));
        }
    }
}
