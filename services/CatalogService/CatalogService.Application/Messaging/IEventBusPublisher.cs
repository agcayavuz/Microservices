namespace CatalogService.Application.Messaging
{
    public interface IEventBusPublisher
    {
        Task PublishAsync<T>(T @event, string? key = null, string? correlationId = null, CancellationToken ct = default);
    }
}
