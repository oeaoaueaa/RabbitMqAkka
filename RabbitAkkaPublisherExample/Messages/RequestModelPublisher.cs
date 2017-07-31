using Akka.Actor;

namespace RabbitAkkaPublisherExample.Messages
{
    class RequestModelPublisher
    {
        public RequestModelPublisher(string exchangeName, string routingKey)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
        }

        public string ExchangeName { get; }
        public string RoutingKey { get; }
    }
}
