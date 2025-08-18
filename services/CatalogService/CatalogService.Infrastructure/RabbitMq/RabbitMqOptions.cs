using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogService.Infrastructure.RabbitMq
{
    public sealed class RabbitMqOptions
    {
        public string HostName { get; init; } = "localhost";
        public int Port { get; init; } = 5672;
        public string VirtualHost { get; init; } = "/";
        public string UserName { get; init; } = "guest";
        public string Password { get; init; } = "guest";

        public string Exchange { get; init; } = "ms.catalog";
        public string ExchangeType { get; init; } = "topic";
        public bool Durable { get; init; } = true;

        // Consumer tarafı örnek:
        public string Queue { get; init; } = "ms.catalog.events";
        public string RoutingKey { get; init; } = "catalog.product.*";
    }
}
