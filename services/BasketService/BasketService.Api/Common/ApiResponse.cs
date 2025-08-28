namespace BasketService.Api.Common
{
    public sealed class ApiError
    {
        public string Code { get; init; } = "error";
        public string Message { get; init; } = "An error occurred.";
        public object? Details { get; init; }
    }

    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public ApiError? Error { get; init; }
        public string? TraceId { get; init; }

        public static ApiResponse<T> Ok(T data, string? traceId = null) =>
            new() { Success = true, Data = data, TraceId = traceId };

        public static ApiResponse<T> Fail(string code, string message, object? details = null, string? traceId = null) =>
            new() { Success = false, Error = new ApiError { Code = code, Message = message, Details = details }, TraceId = traceId };
    }
}
