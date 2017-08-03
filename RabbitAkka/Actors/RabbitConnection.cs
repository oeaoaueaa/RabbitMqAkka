using System;
using Akka.Actor;
using RabbitAkka.Messages;
using RabbitAkka.Messages.Dtos;
using RabbitMQ.Client;

namespace RabbitAkka.Actors
{
    public class RabbitConnection : ReceiveActor
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _conn;

        public static Props CreateProps(IConnectionFactory connectionFactory)
        {
            return Props.Create<RabbitConnection>(connectionFactory);
        }

        public RabbitConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            Ready();
        }

        private void Ready()
        {
            _conn = _connectionFactory.CreateConnection();
            Receive<IRequestModelConsumer>(requestModel =>
            {
                var model = _conn.CreateModel();

                var rabbitModelConsumerActorRef = Context.System.ActorOf(RabbitModelConsumer.CreateProps(model, requestModel));

                Sender.Tell(rabbitModelConsumerActorRef);
            });
            Receive<IRequestModelConsumerWithConcurrencyControl>(requestModel =>
            {
                var model = _conn.CreateModel();

                var rabbitModelConsumerWithConcurrencyControlActorRef = Context.System.ActorOf(RabbitModelConsumerWithConcurrencyControl.CreateProps(model, requestModel));

                Sender.Tell(rabbitModelConsumerWithConcurrencyControlActorRef);
            });
            Receive<IRequestModelPublisher>(requestModelPublisher =>
            {
                var model = _conn.CreateModel();

                var rabbitModelPublisherActorRef = Context.System.ActorOf(RabbitModelPublisher.CreateProps(model, requestModelPublisher));

                Sender.Tell(rabbitModelPublisherActorRef);
            });
            Receive<IRequestModelPublisherRemoteProcedureCall>(requestModelPublisherRemoteProcedureCall =>
            {
                var model = _conn.CreateModel();

                var responseQueueName = Guid.NewGuid().ToString();
                var routingRpcReplyKey = $"{requestModelPublisherRemoteProcedureCall.RoutingKey}###RPCReply";

                var rabbitModelRemoteProcedureCallPublisherActorRef = Context.System.ActorOf(
                    RabbitModelRemoteProcedureCallPublisher.CreateProps(model, requestModelPublisherRemoteProcedureCall, routingRpcReplyKey));

                var requestModelConsumer = new RequestModelConsumer(
                    requestModelPublisherRemoteProcedureCall.ExchangeName,
                    responseQueueName,
                    routingRpcReplyKey,
                    rabbitModelRemoteProcedureCallPublisherActorRef);

                var rabbitModelConsumerActorRef = Context.System.ActorOf(RabbitModelConsumer.CreateProps(model, requestModelConsumer));
                rabbitModelConsumerActorRef.Tell("start consuming");

                Sender.Tell(rabbitModelRemoteProcedureCallPublisherActorRef);
            });
        }
    }
}
