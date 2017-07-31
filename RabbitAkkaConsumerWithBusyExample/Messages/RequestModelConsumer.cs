using Akka.Actor;

namespace RabbitAkkaConsumerWithBusyExample.Messages
{
    class RequestModelConsumer
    {
        public RequestModelConsumer(string exchangeName, string queueName, string routingKey, int concurrencyLevel, IActorRef messageConsumer)
        {
            ExchangeName = exchangeName;
            QueueName = queueName;
            RoutingKey = routingKey;
            ConcurrencyLevel = concurrencyLevel;
            MessageConsumer = messageConsumer;
        }

        public string ExchangeName { get; }
        public string QueueName { get; }
        public string RoutingKey { get; }
        public int ConcurrencyLevel { get; }

        public IActorRef MessageConsumer { get; }
    }
}
