namespace RabbitAkka.Messages
{
    public class RequestModelPublisherRemoteProcedureCall
    {
        public RequestModelPublisherRemoteProcedureCall(string exchangeName, string routingKey)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
        }

        public string ExchangeName { get; }
        public string RoutingKey { get; }
    }
}