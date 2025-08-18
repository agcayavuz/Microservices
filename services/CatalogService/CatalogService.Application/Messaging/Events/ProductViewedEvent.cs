namespace CatalogService.Application.Messaging.Events
{
    public sealed record ProductViewedEvent(int ProductId,DateTime ViewedAtUtc,string? CorrelationId);
}
