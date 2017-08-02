using Akka.Actor;
using RabbitAkka.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitAkka.Actors
{
    public class RabbitModelConsumer : ReceiveActor
    {
        private readonly IModel _model;
        private readonly RequestModelConsumer _requestModelConsumer;
        private EventingBasicConsumer _consumer;
        private string _consumerTag;
        private IActorRef _self;

        public static Props CreateProps(IModel model, RequestModelConsumer requestModelConsumer)
        {
            return Props.Create<RabbitModelConsumer>(model, requestModelConsumer);
        }

        public RabbitModelConsumer(IModel model, RequestModelConsumer requestModelConsumer)
        {
            _model = model;
            _requestModelConsumer = requestModelConsumer;

            model.QueueDeclare(requestModelConsumer.QueueName, false, true, true);            


            ReceiveAny(_ =>
            {
                _self = Self;
                Become(Ready);
            });
        }

        private void Ready()
        {
            _model.QueueBind(_requestModelConsumer.QueueName, _requestModelConsumer.ExchangeName, _requestModelConsumer.RoutingKey);
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += (ch, ea) =>
            {
                _self.Tell(ea);
            };
            _consumerTag = _model.BasicConsume(_requestModelConsumer.QueueName, false, _consumer);

            Receive<BasicDeliverEventArgs>(basicDeliverEventArgs =>
            {
                _requestModelConsumer.MessageConsumer.Tell(new ConsumedMessage(basicDeliverEventArgs.Body, basicDeliverEventArgs));
            });
        }
    }
}