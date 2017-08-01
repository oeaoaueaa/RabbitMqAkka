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
            Receive<PublishMessage>(publishMessage =>
            {
                _model.BasicPublish(_requestModelPublisher.ExchangeName, _requestModelPublisher.RoutingKey, false, null, publishMessage.Message);
            });
        }
    }
}