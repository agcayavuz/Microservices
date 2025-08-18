using CatalogService.Application.Messaging;
using System.Text;
using System.Text.Json;

namespace CatalogService.Infrastructure.Messaging
{
    public sealed class JsonMessageSerializer : IMessageSerializer
    {
        private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public byte[] Serialize<T>(T value) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Options));

        public T? Deserialize<T>(byte[] payload) => JsonSerializer.Deserialize<T>(payload, Options);
    }
}
