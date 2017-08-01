using System;
using Akka.Actor;
using RabbitAkka.Messages;
using RabbitMQ.Client;

namespace RabbitAkka.Actors
{
    class RabbitModelRemoteProcedureCallPublisher : ReceiveActor, IWithUnboundedStash
    {
        private readonly IModel _model;
        private readonly RequestModelPublisherRemoteProcedureCall _requestModelPublisherRemoteProcedureCall;

        public static Props CreateProps(IModel model, RequestModelPublisherRemoteProcedureCall requestModelPublisherRemoteProcedureCall)
        {
            return Props.Create<RabbitModelPublisher>(model, requestModelPublisherRemoteProcedureCall);
        }

        public RabbitModelRemoteProcedureCallPublisher(IModel model, RequestModelPublisherRemoteProcedureCall requestModelPublisherRemoteProcedureCall)
        {
            _model = model;
            _requestModelPublisherRemoteProcedureCall = requestModelPublisherRemoteProcedureCall;

            Ready();
        }

        private void Ready()
        {
            Receive<PublishMessage>(publishMessage =>
            {
                var corrId = Guid.NewGuid().ToString();
                var props = _model.CreateBasicProperties();

                var replyQueueName = ""; // TODO
                props.ReplyTo = replyQueueName;
                props.CorrelationId = corrId;


                _model.BasicPublish(
                    _requestModelPublisherRemoteProcedureCall.ExchangeName, 
                    _requestModelPublisherRemoteProcedureCall.RoutingKey, 
                    false, 
                    null, 
                    publishMessage.Message);
            });
        }

        public IStash Stash { get; set; }
    }
}