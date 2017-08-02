namespace RabbitAkka.Messages
{
    public class PublishMessageUsingRoutingKey
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
