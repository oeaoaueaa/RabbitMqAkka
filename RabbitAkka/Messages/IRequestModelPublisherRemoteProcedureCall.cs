using Akka.Actor;

namespace RabbitAkka.Messages
{
    public interface IRequestModelPublisherRemoteProcedureCall
    {
        string ExchangeName { get; }
        IActorRef MessageConsumer { get; }
        string RoutingKey { get; }
    }
}