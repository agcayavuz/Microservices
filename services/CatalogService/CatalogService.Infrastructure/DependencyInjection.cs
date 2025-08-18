using CatalogService.Application.Abstractions;
using CatalogService.Application.Messaging;
using CatalogService.Infrastructure.Messaging;
using CatalogService.Infrastructure.RabbitMq;
using CatalogService.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // InMemory repos
            services.AddScoped<IProductRepository, InMemoryProductRepository>();
            services.AddScoped<IBrandRepository, InMemoryBrandRepository>();
            services.AddScoped<ICategoryRepository, InMemoryCategoryRepository>();

            // Serializer
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

            // RabbitMQ options bind
            var opt = new RabbitMqOptions();
            configuration.GetSection("RabbitMq").Bind(opt);
            services.AddSingleton(opt);

            // RabbitMQ
            services.AddSingleton<RabbitMqConnection>();
            services.AddScoped<IEventBusPublisher, RabbitMqEventBusPublisher>();
            services.AddHostedService<RabbitMqConsumerHostedService>(); // basit log consumer

            return services;
        }
    }
}
