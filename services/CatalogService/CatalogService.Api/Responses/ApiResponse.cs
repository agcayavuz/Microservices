namespace CatalogService.Api.Responses
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public string? Error { get; init; }
        public string? CorrelationId { get; init; }

        public static ApiResponse<T> Ok(T data, string? correlationId = null)
            => new() { Success = true, Data = data, CorrelationId = correlationId };

        public static ApiResponse<T> Fail(string error, string? correlationId = null)
            => new() { Success = false, Error = error, CorrelationId = correlationId };
    }
}
