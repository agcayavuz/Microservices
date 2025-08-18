using CatalogService.Application.Messaging;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CatalogService.Infrastructure.Messaging
{
    public sealed class NoOpEventBusPublisher(ILogger<NoOpEventBusPublisher> logger) : IEventBusPublisher
    {
        public Task PublishAsync<T>(T @event, string? key = null, string? correlationId = null, CancellationToken ct = default)
        {
            // Dummy faz: sadece loglayalım
            var payload = JsonSerializer.Serialize(@event);
            logger.LogInformation("NoOp publish. Key={Key}, EventType={Type}, Payload={Payload}, CorrelationId={correlationId}",
                key, typeof(T).Name, payload, correlationId);
            return Task.CompletedTask;
        }
    }
}
