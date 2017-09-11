using System;
using Akka.Actor;
using RabbitAkka.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitAkka.Actors
{
    public class RabbitModelPublisher : ReceiveActor, IWithUnboundedStash
    {
        private readonly IModel _model;
        private readonly IRequestModelPublisher _requestModelPublisher;
        private IActorRef _self;        

        public static Props CreateProps(IModel model, IRequestModelPublisher requestModelPublisher)
        {
            return Props.Create<RabbitModelPublisher>(model, requestModelPublisher);
        }

        public RabbitModelPublisher(IModel model, IRequestModelPublisher requestModelPublisher)
        {
            _model = model;
            _requestModelPublisher = requestModelPublisher;

            if (_requestModelPublisher.WaitForPublishAcks)
            {
                _model.ConfirmSelect();
                _model.BasicAcks +=
                    (sender, args) =>
                    {
                        Console.WriteLine(
                            $"{nameof(RabbitModelPublisher)}: BasicAck: {args.DeliveryTag} {args.Multiple}"); // TODO DEBUG REMOVE
                        _self.Tell(args, null);
                    };
            }

            Ready();
        }

        private void Ready()
        {
            void WaitForAckIfNeeded()
            {
                if (_requestModelPublisher.WaitForPublishAcks)
                {
                    Become(WaitingForAck);
                }
            }

            Receive<IPublishMessageUsingRoutingKey>(publishMessageUsingRoutingKey =>
            {
                _model.BasicPublish(publishMessageUsingRoutingKey.ExchangeName,
                    publishMessageUsingRoutingKey.RoutingKey, false, null, publishMessageUsingRoutingKey.Message);
                WaitForAckIfNeeded();
            });
            Receive<IPublishMessageUsingPublicationAddress>(publishMessageUsingPublicationAddress =>
            {
                // TODO needs correlation id!
                _model.BasicPublish(publishMessageUsingPublicationAddress.PublicationAddress, null,
                    publishMessageUsingPublicationAddress.Message);
                WaitForAckIfNeeded();
            });
            Receive<IPublishMessageToQueue>(publishMessageToQueue =>
            {
                _model.BasicPublish(string.Empty,
                    publishMessageToQueue.QueueName, false, null, publishMessageToQueue.Message);
                WaitForAckIfNeeded();
            });
        }


        #region WaitingForAck

        private void WaitingForAck()
        {
            // TODO schedule timeout!
            Receive<BasicAckEventArgs>(basicAckEventArgs =>
            {
                if (_model.NextPublishSeqNo -1 <= basicAckEventArgs.DeliveryTag) // only one deliver at a time
                {
                    Become(Ready);
                    Stash.UnstashAll();
                }
            });
            ReceiveAny(message => Stash.Stash());
        }
        #endregion WaitingForAck

        protected override void PreStart()
        {
            _self = Self;
        }

        public IStash Stash { get; set; }
    }
}