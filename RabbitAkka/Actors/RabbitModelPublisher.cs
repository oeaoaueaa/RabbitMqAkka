using Akka.Actor;
using RabbitAkka.Messages;
using RabbitMQ.Client;

namespace RabbitAkka.Actors
{
    public class RabbitModelPublisher : ReceiveActor
    {
        private readonly IModel _model;
        private readonly RequestModelPublisher _requestModelPublisher;

        public static Props CreateProps(IModel model, RequestModelPublisher requestModelPublisher)
        {
            return Props.Create<RabbitModelPublisher>(model, requestModelPublisher);
        }

        public RabbitModelPublisher(IModel model, RequestModelPublisher requestModelPublisher)
        {
            _model = model;
            _requestModelPublisher = requestModelPublisher;

            Ready();
        }

        private void Ready()
        {
            Receive<PublishMessageUsingRoutingKey>(publishMessageUsingRoutingKey =>
            {
                _model.BasicPublish(publishMessageUsingRoutingKey.ExchangeName,
                    publishMessageUsingRoutingKey.RoutingKey, false, null, publishMessageUsingRoutingKey.Message);
            });
            Receive<PublishMessageUsingPublicationAddress>(publishMessageUsingPublicationAddress =>
            {
                // TODO needs correlation id!
                _model.BasicPublish(publishMessageUsingPublicationAddress.PublicationAddress, null,
                    publishMessageUsingPublicationAddress.Message);
            });
        }
    }
}