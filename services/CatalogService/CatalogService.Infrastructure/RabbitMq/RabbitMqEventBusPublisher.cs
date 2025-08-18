using CatalogService.Application.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

namespace CatalogService.Infrastructure.RabbitMq
{


    public sealed class RabbitMqEventBusPublisher(
        RabbitMqConnection connection,
        RabbitMqOptions options,
        IMessageSerializer serializer,
        ILogger<RabbitMqEventBusPublisher> logger) : IEventBusPublisher
    {
        public Task PublishAsync<T>(T @event, string? key = null, string? correlationId = null, CancellationToken ct = default)
        {
            using var channel = connection.CreateChannel();

            channel.ExchangeDeclare(exchange: options.Exchange, type: options.ExchangeType, durable: options.Durable, autoDelete: false);

            var body = serializer.Serialize(@event);
            var props = channel.CreateBasicProperties();
            props.Persistent = true;

            // CorrelationId alanını doldur
            if (!string.IsNullOrWhiteSpace(correlationId))
                props.CorrelationId = correlationId;

            // Header'ı UTF8 byte[] olarak yaz
            props.Headers ??= new Dictionary<string, object>();
            if (!props.Headers.ContainsKey("X-Correlation-Id") && !string.IsNullOrWhiteSpace(correlationId))
                props.Headers["X-Correlation-Id"] = Encoding.UTF8.GetBytes(correlationId!);

            var routingKey = key ?? options.RoutingKey;

            channel.BasicPublish(
                exchange: options.Exchange,
                routingKey: routingKey,
                basicProperties: props,
                body: body
            );

            logger.LogInformation("Published to RabbitMQ. Exchange={Exchange}, Key={Key}, Size={Size} bytes",
                options.Exchange, routingKey, body.Length);

            return Task.CompletedTask;
        }
    }
}
