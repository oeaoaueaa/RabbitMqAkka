using Akka.Actor;

namespace RabbitAkka.Messages
{
    public interface IRequestModelConsumer
    {
        string ExchangeName { get; }
        IActorRef MessageConsumer { get; }
        string QueueName { get; }
        string RoutingKey { get; }
    }
}