using RabbitMQ.Client;

namespace CatalogService.Infrastructure.RabbitMq
{
    public sealed class RabbitMqConnection : IDisposable
    {
        private readonly IConnection _connection;

        public RabbitMqConnection(RabbitMqOptions opt)
        {
            var factory = new ConnectionFactory
            {
                HostName = opt.HostName,
                Port = opt.Port,
                VirtualHost = opt.VirtualHost,
                UserName = opt.UserName,
                Password = opt.Password,
                DispatchConsumersAsync = true
            };
            _connection = factory.CreateConnection();
        }

        public IModel CreateChannel() => _connection.CreateModel();

        public void Dispose() => _connection.Dispose();
    }
}
