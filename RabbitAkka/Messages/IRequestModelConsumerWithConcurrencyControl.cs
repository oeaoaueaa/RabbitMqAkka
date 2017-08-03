using Akka.Actor;

namespace RabbitAkka.Messages
{
    public interface IRequestModelConsumerWithConcurrencyControl
    {
        int ConcurrencyLevel { get; }
        string ExchangeName { get; }
        IActorRef MessageConsumer { get; }
        string QueueName { get; }
        string RoutingKey { get; }
    }
}