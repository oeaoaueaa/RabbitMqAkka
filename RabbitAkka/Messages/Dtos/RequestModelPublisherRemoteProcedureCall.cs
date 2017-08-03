using Akka.Actor;

namespace RabbitAkka.Messages.Dtos
{
    public class RequestModelPublisherRemoteProcedureCall : IRequestModelPublisherRemoteProcedureCall
    {
        public RequestModelPublisherRemoteProcedureCall(string exchangeName, string routingKey, IActorRef messageConsumer)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            MessageConsumer = messageConsumer;
        }

        public string ExchangeName { get; }
        public string RoutingKey { get; }
        public IActorRef MessageConsumer { get; }
    }
}