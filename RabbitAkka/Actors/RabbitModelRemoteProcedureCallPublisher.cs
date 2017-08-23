using System;
using Akka.Actor;
using RabbitAkka.Messages;
using RabbitAkka.Messages.Dtos;
using RabbitMQ.Client;

namespace RabbitAkka.Actors
{
    public class RabbitModelRemoteProcedureCallPublisher : ReceiveActor, IWithUnboundedStash
    {
        private readonly IModel _model;
        private readonly IRequestModelPublisherRemoteProcedureCall _requestModelPublisherRemoteProcedureCall;
        private readonly string _routingRpcReplyKey;

        public static Props CreateProps(IModel model, IRequestModelPublisherRemoteProcedureCall requestModelPublisherRemoteProcedureCall, string routingRpcReplyKey)
        {
            return Props.Create<RabbitModelRemoteProcedureCallPublisher>(model, requestModelPublisherRemoteProcedureCall, routingRpcReplyKey);
        }

        public RabbitModelRemoteProcedureCallPublisher(IModel model, IRequestModelPublisherRemoteProcedureCall requestModelPublisherRemoteProcedureCall, string routingRpcReplyKey)
        {
            _model = model;
            _requestModelPublisherRemoteProcedureCall = requestModelPublisherRemoteProcedureCall;
            _routingRpcReplyKey = routingRpcReplyKey;

            Ready();
        }

        private void Ready()
        {
            Receive<IPublishMessageUsingRoutingKey>(publishMessage =>
            {
                var corrId = Guid.NewGuid().ToString();
                var props = _model.CreateBasicProperties();

                //props.ReplyTo = _responseQueueName;
                props.CorrelationId = corrId;
                props.ReplyToAddress = new PublicationAddress(ExchangeType.Topic, // TODO, is better to use one queue per publisher
                    _requestModelPublisherRemoteProcedureCall.ExchangeName,
                    _routingRpcReplyKey);

                _model.BasicPublish(
                    _requestModelPublisherRemoteProcedureCall.ExchangeName, 
                    _requestModelPublisherRemoteProcedureCall.RoutingKey, 
                    false,
                    props, 
                    publishMessage.Message);
            });
            Receive<IPublishMessageToQueue>(publishMessageToQueue =>
            {
                var corrId = Guid.NewGuid().ToString();
                var props = _model.CreateBasicProperties();
                //TODO CREATE A QUEUE AND ASK TO REPLY THERE :)
                props.CorrelationId = corrId;
                
                props.ReplyToAddress = new PublicationAddress(ExchangeType.Topic, // TODO, is better to use one queue per publisher
                    _requestModelPublisherRemoteProcedureCall.ExchangeName,
                    _routingRpcReplyKey);

                _model.BasicPublish(
                    string.Empty,
                    publishMessageToQueue.QueueName,
                    false,
                    props,
                    publishMessageToQueue.Message);
            });
            Receive<IConsumedMessage>(consumedMessage =>
            {
                _requestModelPublisherRemoteProcedureCall.MessageConsumer.Tell(new ConsumedMessage(consumedMessage.Message, consumedMessage.BasicDeliverEventArgs));
            });
        }

        public IStash Stash { get; set; }
    }
}