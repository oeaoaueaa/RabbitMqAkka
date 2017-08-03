namespace RabbitAkka.Messages.Dtos
{
    public class PublishMessageUsingRoutingKey : IPublishMessageUsingRoutingKey
    {
        public PublishMessageUsingRoutingKey(string exchangeName, string routingKey, byte[] message)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            Message = message;
        }

        public string ExchangeName { get; }
        public string RoutingKey { get; }
        public byte[] Message { get; }
    }
}
