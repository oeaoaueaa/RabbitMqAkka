using Akka.Actor;
using RabbitAkka.Messages;
using RabbitAkka.Messages.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitAkka.Actors
{
    public class RabbitModelConsumerWithConcurrencyControl : ReceiveActor
    {
        private readonly IModel _model;
        private readonly IRequestModelConsumerWithConcurrencyControl _requestModelConsumerWithConcurrencyControl;
        private EventingBasicConsumer _consumer;
        private string _consumerTag;
        private int _concurrencyCapacity;
        private IActorRef _self;

        public static Props CreateProps(IModel model, IRequestModelConsumerWithConcurrencyControl requestModelConsumerWithConcurrencyControl)
        {
            return Props.Create<RabbitModelConsumerWithConcurrencyControl>(model, requestModelConsumerWithConcurrencyControl);
        }

        public RabbitModelConsumerWithConcurrencyControl(IModel model, IRequestModelConsumerWithConcurrencyControl requestModelConsumerWithConcurrencyControl)
        {
            _model = model;
            _requestModelConsumerWithConcurrencyControl = requestModelConsumerWithConcurrencyControl;

            model.QueueDeclare(requestModelConsumerWithConcurrencyControl.QueueName, false, true, true);            


            ReceiveAny(_ =>
            {
                _self = Self;
                Become(Ready);
            });
        }

        private void Ready()
        {
            _concurrencyCapacity = _requestModelConsumerWithConcurrencyControl.ConcurrencyLevel;
            _model.QueueBind(_requestModelConsumerWithConcurrencyControl.QueueName, _requestModelConsumerWithConcurrencyControl.ExchangeName, _requestModelConsumerWithConcurrencyControl.RoutingKey);
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += (ch, ea) =>
            {
                _self.Tell(ea);
            };
            _consumerTag = _model.BasicConsume(_requestModelConsumerWithConcurrencyControl.QueueName, false, _consumer);

            Receive<BasicDeliverEventArgs>(basicDeliverEventArgs =>
            {
                if (_concurrencyCapacity > 0)
                {
                    _concurrencyCapacity--;
                    // TODO handle timeouts, use basicDeliverEventArgs.DeliveryTag to track requests
                    _requestModelConsumerWithConcurrencyControl.MessageConsumer.Tell(new ConsumedMessage(basicDeliverEventArgs.Body, basicDeliverEventArgs));
                    _model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
                }
                else
                {
                    _model.BasicNack(basicDeliverEventArgs.DeliveryTag, true, true);
                }
            });
            
            Receive<IMessageProcessed>(processed =>
            {
                _concurrencyCapacity++;
            });
        }
    }
}