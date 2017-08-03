namespace RabbitAkka.Messages
{
    public interface IPublishMessageUsingRoutingKey
    {
        string ExchangeName { get; }
        byte[] Message { get; }
        string RoutingKey { get; }
    }
}