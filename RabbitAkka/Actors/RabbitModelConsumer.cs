using System;
using Akka.Actor;
using RabbitAkka.Messages;
using RabbitAkka.Messages.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitAkka.Actors
{
    public class RabbitModelConsumer : ReceiveActor
    {
        private readonly IModel _model;
        private readonly IRequestModelConsumer _requestModelConsumer;
        private EventingBasicConsumer _consumer;
        private string _consumerTag;
        private IActorRef _self;

        public static Props CreateProps(IModel model, IRequestModelConsumer requestModelConsumer)
        {
            return Props.Create<RabbitModelConsumer>(model, requestModelConsumer);
        }

        public RabbitModelConsumer(IModel model, IRequestModelConsumer requestModelConsumer)
        {
            _model = model;
            _requestModelConsumer = requestModelConsumer;

            model.QueueDeclare(requestModelConsumer.QueueName, false, true, true);            


            ReceiveAny(_ =>
            {
                try
                {
                    _self = Self;

                    Become(Ready);
                    Sender.Tell(true);
                }
                catch (Exception)
                {
                    Sender.Tell(false);
                }
            });
        }

        private void Ready()
        {
            if (!string.IsNullOrEmpty(_requestModelConsumer.ExchangeName))
                _model.QueueBind(_requestModelConsumer.QueueName, _requestModelConsumer.ExchangeName, _requestModelConsumer.RoutingKey);
            
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += (ch, ea) =>
            {
                _self.Tell(ea);
            };
            
            _consumerTag = _model.BasicConsume(_requestModelConsumer.QueueName, false, "", false, false, null, _consumer);

            Receive<BasicDeliverEventArgs>(basicDeliverEventArgs =>
            {
                _requestModelConsumer.MessageConsumer.Tell(new ConsumedMessage(basicDeliverEventArgs.Body, basicDeliverEventArgs));
            });
        }
    }
}