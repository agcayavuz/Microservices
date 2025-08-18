using CatalogService.Application.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace CatalogService.Infrastructure.RabbitMq
{


    public sealed class RabbitMqConsumerHostedService(
        RabbitMqConnection connection,
        RabbitMqOptions options,
        IMessageSerializer serializer,
        ILogger<RabbitMqConsumerHostedService> logger) : BackgroundService
    {
        private IModel? _channel;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = connection.CreateChannel();

            _channel.ExchangeDeclare(exchange: options.Exchange, type: options.ExchangeType, durable: options.Durable, autoDelete: false);

            _channel.QueueDeclare(queue: options.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queue: options.Queue, exchange: options.Exchange, routingKey: options.RoutingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var body = ea.Body.ToArray();

                // 1) BasicProperties.CorrelationId varsa onu kullan
                var corrId = ea.BasicProperties?.CorrelationId;

                // 2) Yoksa header’dan UTF8 decode et
                if (string.IsNullOrWhiteSpace(corrId) && ea.BasicProperties?.Headers is { } headers &&
                    headers.TryGetValue("X-Correlation-Id", out var hdrVal))
                {
                    corrId = TryDecodeHeader(hdrVal);
                }

                logger.LogInformation("RabbitMQ consumed. Key={Key}, Size={Size} bytes, CorrelationId={CorrelationId}",
                    ea.RoutingKey, body.Length, corrId ?? "-");

                _channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                await Task.CompletedTask;
            };

            _channel.BasicQos(0, 10, false);
            _channel.BasicConsume(queue: options.Queue, autoAck: false, consumer: consumer);

            logger.LogInformation("RabbitMQ consumer started: Queue={Queue}, RoutingKey={Key}", options.Queue, options.RoutingKey);
            return Task.CompletedTask;
        }

        private static string? TryDecodeHeader(object value)
        {
            try
            {
                return value switch
                {
                    byte[] b => Encoding.UTF8.GetString(b),
                    ReadOnlyMemory<byte> m => Encoding.UTF8.GetString(m.ToArray()),
                    // Bazı sürümlerde Amqp types farklı gelebilir; güvenli fallback
                    _ => value?.ToString()
                };
            }
            catch
            {
                return null;
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            base.Dispose();
        }
    }
}
