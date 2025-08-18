namespace CatalogService.Application.Messaging
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T value);
        T? Deserialize<T>(byte[] payload);
    }
}
