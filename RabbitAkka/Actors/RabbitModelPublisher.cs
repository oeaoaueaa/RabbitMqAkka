using Akka.Actor;
using RabbitAkka.Messages;
using RabbitMQ.Client;

namespace RabbitAkka.Actors
{
    public class RabbitModelPublisher : ReceiveActor
    {
        private readonly IModel _model;

        public static Props CreateProps(IModel model, IRequestModelPublisher requestModelPublisher)
        {
            return Props.Create<RabbitModelPublisher>(model, requestModelPublisher);
        }

        public RabbitModelPublisher(IModel model, IRequestModelPublisher requestModelPublisher)
        {
            _model = model;

            Ready();
        }

        private void Ready()
        {
            Receive<IPublishMessageUsingRoutingKey>(publishMessageUsingRoutingKey =>
            {
                _model.BasicPublish(publishMessageUsingRoutingKey.ExchangeName,
                    publishMessageUsingRoutingKey.RoutingKey, false, null, publishMessageUsingRoutingKey.Message);
            });
            Receive<IPublishMessageUsingPublicationAddress>(publishMessageUsingPublicationAddress =>
            {
                // TODO needs correlation id!
                _model.BasicPublish(publishMessageUsingPublicationAddress.PublicationAddress, null,
                    publishMessageUsingPublicationAddress.Message);
            });
            Receive<IPublishMessageToQueue>(publishMessageToQueue =>
            {
                _model.BasicPublish(string.Empty,
                    publishMessageToQueue.QueueName, false, null, publishMessageToQueue.Message);
            });
        }
    }
}