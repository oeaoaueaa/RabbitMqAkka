using Akka.Actor;

namespace RabbitAkka.Messages.Dtos
{
    public class RequestModelConsumerWithConcurrencyControl : IRequestModelConsumerWithConcurrencyControl
    {
        public RequestModelConsumerWithConcurrencyControl(string exchangeName, string queueName, string routingKey, int concurrencyLevel, IActorRef messageConsumer)
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
