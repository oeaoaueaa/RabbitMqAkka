using System;
using System.Collections.Generic;
using Akka.Actor;
using RabbitAkka.Messages;
using RabbitAkka.Messages.Dtos;
using RabbitAkka.Messages.Dtos.Supervision;
using RabbitMQ.Client;

namespace RabbitAkka.Actors
{
    public class RabbitConnection : ReceiveActor, IWithUnboundedStash
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _conn;

        private readonly List<IActorRef> _publishersActorRefs;
        private readonly List<IActorRef> _consumersActorRefs;

        public static Props CreateProps(IConnectionFactory connectionFactory)
        {
            return Props.Create<RabbitConnection>(connectionFactory);
        }

        public RabbitConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;

            _publishersActorRefs = new List<IActorRef>();
            _consumersActorRefs = new List<IActorRef>();

            Become(Ready);
        }

        protected override void PreStart()
        {
            var self = Context.Self;
            // TODO dispose previous conn if any
            _conn = _connectionFactory.CreateConnection();
            // TODO dispose event handlers
            _conn.ConnectionShutdown += (sender, args) => { self.Tell(new ConnectionShutdown(args)); };
            _conn.RecoverySucceeded += (sender, args) => { self.Tell(new ConnectionRecoverySucceeded()); };            
        }

        private void Ready()
        {
            ReadyForConnectionEvents();
            ReadyForConsumers();
            ReadyForPublishers();            
        }

        private void ReadyForConsumers()
        {
            Receive<IRequestModelConsumer>(requestModel =>
            {
                var model = _conn.CreateModel();

                var rabbitModelConsumerActorRef =
                    Context.System.ActorOf(RabbitModelConsumer.CreateProps(model, requestModel));

                _consumersActorRefs.Add(rabbitModelConsumerActorRef);

                var consumerSupervisedActorRef =
                    Context.System.ActorOf(RabbitActorSupervisor.CreateProps(rabbitModelConsumerActorRef));
                _consumersActorRefs.Add(consumerSupervisedActorRef);

                Sender.Tell(consumerSupervisedActorRef);
            });
            Receive<IRequestModelConsumerWithConcurrencyControl>(requestModel =>
            {
                var model = _conn.CreateModel();

                var rabbitModelConsumerWithConcurrencyControlActorRef =
                    Context.System.ActorOf(RabbitModelConsumerWithConcurrencyControl.CreateProps(model, requestModel));

                _consumersActorRefs.Add(rabbitModelConsumerWithConcurrencyControlActorRef);

                var consumerSupervisedActorRef =
                    Context.System.ActorOf(RabbitActorSupervisor.CreateProps(rabbitModelConsumerWithConcurrencyControlActorRef));
                _consumersActorRefs.Add(consumerSupervisedActorRef);

                Sender.Tell(consumerSupervisedActorRef);
            });
        }

        private void ReadyForPublishers()
        {
            Receive<IRequestModelPublisher>(requestModelPublisher =>
            {
                var model = _conn.CreateModel();

                var rabbitModelPublisherActorRef =
                    Context.System.ActorOf(RabbitModelPublisher.CreateProps(model, requestModelPublisher));

                var supervisedActorRef =
                    Context.System.ActorOf(RabbitActorSupervisor.CreateProps(rabbitModelPublisherActorRef));

                _publishersActorRefs.Add(supervisedActorRef);
                Sender.Tell(supervisedActorRef);
            });
            Receive<IRequestModelPublisherRemoteProcedureCall>(requestModelPublisherRemoteProcedureCall =>
            {
                IActorRef publisherActorRef;
                var model = _conn.CreateModel();
                var responseQueueName = Guid.NewGuid().ToString();
                var routingRpcReplyKey = $"{requestModelPublisherRemoteProcedureCall.RoutingKey}###RPCReply";

                if (string.IsNullOrEmpty(requestModelPublisherRemoteProcedureCall.ExchangeName))
                {
                    var modelRemoteProcedureCallPublisherWithDirectQueueConsumer
                        = new ModelRemoteProcedureCallPublisherWithDirectQueueConsumer(
                            requestModelPublisherRemoteProcedureCall, responseQueueName);

                    var rabbitModelRemoteProcedureCallPublisherActorRef = Context.System.ActorOf(
                        RabbitModelRemoteProcedureCallPublisher.CreateProps(model,
                            modelRemoteProcedureCallPublisherWithDirectQueueConsumer));

                    publisherActorRef = rabbitModelRemoteProcedureCallPublisherActorRef;
                }
                else
                {
                    var modelRemoteProcedureCallPublisherWithTopicExchangeConsumer
                        = new ModelRemoteProcedureCallPublisherWithTopicExchangeConsumer(
                            requestModelPublisherRemoteProcedureCall,
                            new PublicationAddress(ExchangeType.Topic,
                                requestModelPublisherRemoteProcedureCall.ExchangeName, routingRpcReplyKey));

                    var rabbitModelRemoteProcedureCallPublisherActorRef = Context.System.ActorOf(
                        RabbitModelRemoteProcedureCallPublisher.CreateProps(model,
                            modelRemoteProcedureCallPublisherWithTopicExchangeConsumer));

                    publisherActorRef = rabbitModelRemoteProcedureCallPublisherActorRef;
                }

                var publisherSupervisedActorRef =
                    Context.System.ActorOf(RabbitActorSupervisor.CreateProps(publisherActorRef));

                var requestModelConsumer = new RequestModelConsumer(
                    requestModelPublisherRemoteProcedureCall.ExchangeName,
                    responseQueueName,
                    routingRpcReplyKey,
                    publisherSupervisedActorRef);

                var rabbitModelConsumerActorRef = // TODO need to add supervisor for consumer too
                    Context.System.ActorOf(RabbitModelConsumer.CreateProps(model, requestModelConsumer));
                var consumerSupervisedActorRef =
                    Context.System.ActorOf(RabbitActorSupervisor.CreateProps(rabbitModelConsumerActorRef));

                consumerSupervisedActorRef.Tell("start consuming"); // TODO restart consumers on resume

                _consumersActorRefs.Add(consumerSupervisedActorRef);
                _publishersActorRefs.Add(publisherSupervisedActorRef);
                Sender.Tell(publisherSupervisedActorRef);
            });
        }

        private void ReadyForConnectionEvents()
        {
            Receive<ConnectionShutdown>(connectionShutdown =>
            {
                var pauseProcessingMessage = new PauseProcessing(); // TODO can use pause transaction id
                _publishersActorRefs.ForEach(ar => ar.Tell(pauseProcessingMessage));
                _consumersActorRefs.ForEach(ar => ar.Tell(pauseProcessingMessage));
                Become(AwaitingConnectionRecovery);
            });
        }

        private void AwaitingConnectionRecovery()
        {
            Receive<ConnectionRecoverySucceeded>(connectionRecoverySucceeded =>
            {
                var resumeProcessingMessage = new ResumeProcessing(); // TODO can use pause transaction id
                _publishersActorRefs.ForEach(ar => ar.Tell(resumeProcessingMessage));
                _consumersActorRefs.ForEach(ar => ar.Tell(resumeProcessingMessage));
                Become(Ready);
                Stash.UnstashAll();
            });
            Receive<ConnectionShutdown>(connectionShutdown =>
            {
                // TODO ignore/log
            });
            ReceiveAny(m => Stash.Stash());
        }

        private class ConnectionShutdown
        {
            public ShutdownEventArgs ShutdownEventArgs { get; }

            public ConnectionShutdown(ShutdownEventArgs shutdownEventArgs)
            {
                ShutdownEventArgs = shutdownEventArgs;
            }
        }

        private class ConnectionRecoverySucceeded
        {
        }

        public IStash Stash { get; set; }
    }
}
