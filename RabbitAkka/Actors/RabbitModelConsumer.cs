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
        private int _concurrencyCapacity;
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
            _concurrencyCapacity = _requestModelConsumer.ConcurrencyLevel;
            _model.QueueBind(_requestModelConsumer.QueueName, _requestModelConsumer.ExchangeName, _requestModelConsumer.RoutingKey);
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += (ch, ea) =>
            {
                _self.Tell(ea);
            };
            _consumerTag = _model.BasicConsume(_requestModelConsumer.QueueName, false, _consumer);

            Receive<BasicDeliverEventArgs>(basicDeliverEventArgs =>
            {
                if (_concurrencyCapacity > 0)
                {
                    _concurrencyCapacity--;
                    // TODO handle timeouts, use basicDeliverEventArgs.DeliveryTag to track requests
                    _requestModelConsumer.MessageConsumer.Tell(basicDeliverEventArgs.Body);
                    _model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
                }
                else
                {
                    _model.BasicNack(basicDeliverEventArgs.DeliveryTag, true, true);
                }
            });
            
            Receive<MessageProcessed>(processed =>
            {
                _concurrencyCapacity++;
            });
        }
    }
}