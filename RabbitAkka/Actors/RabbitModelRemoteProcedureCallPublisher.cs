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
        
        // Only one Topic or Direct config will be passed
        private readonly IModelRemoteProcedureCallPublisherWithTopicExchangeConsumer
            _modelRemoteProcedureCallPublisherWithTopicExchangeConsumer;

        private readonly IModelRemoteProcedureCallPublisherWithDirectQueueConsumer
            _modelRemoteProcedureCallPublisherWithDirectQueueConsumer;

        public static Props CreateProps(IModel model, IModelRemoteProcedureCallPublisherWithTopicExchangeConsumer modelRemoteProcedureCallPublisherWithTopicExchangeConsumer)
        {
            return Props.Create<RabbitModelRemoteProcedureCallPublisher>(model, modelRemoteProcedureCallPublisherWithTopicExchangeConsumer);
        }

        public RabbitModelRemoteProcedureCallPublisher(IModel model, IModelRemoteProcedureCallPublisherWithTopicExchangeConsumer modelRemoteProcedureCallWithTopicExchangeConsumerPublisherWithTopicExchangeConsumer)
        {
            _model = model;
            _modelRemoteProcedureCallPublisherWithTopicExchangeConsumer = modelRemoteProcedureCallWithTopicExchangeConsumerPublisherWithTopicExchangeConsumer;

            Ready();
        }

        public static Props CreateProps(IModel model, IModelRemoteProcedureCallPublisherWithDirectQueueConsumer modelRemoteProcedureCallPublisherWithDirectQueueConsumer)
        {
            return Props.Create<RabbitModelRemoteProcedureCallPublisher>(model, modelRemoteProcedureCallPublisherWithDirectQueueConsumer);
        }

        public RabbitModelRemoteProcedureCallPublisher(IModel model, IModelRemoteProcedureCallPublisherWithDirectQueueConsumer modelRemoteProcedureCallPublisherWithDirectQueueConsumer)
        {
            _model = model;
            _modelRemoteProcedureCallPublisherWithDirectQueueConsumer = modelRemoteProcedureCallPublisherWithDirectQueueConsumer;

            Ready();
        }

        private void Ready()
        {
            Receive<IPublishMessageUsingRoutingKey>(publishMessage =>
            {
                var props = CreateProps();

                _model.BasicPublish(
                    publishMessage.ExchangeName,
                    publishMessage.RoutingKey,
                    false,
                    props, 
                    publishMessage.Message);
            });
            Receive<IPublishMessageToQueue>(publishMessageToQueue =>
            {
                var props = CreateProps();

                _model.BasicPublish(
                    string.Empty,
                    publishMessageToQueue.QueueName,
                    false,
                    props,
                    publishMessageToQueue.Message);
            });
            Receive<IConsumedMessage>(consumedMessage =>
            {
                if (_modelRemoteProcedureCallPublisherWithTopicExchangeConsumer != null)
                {
                    _modelRemoteProcedureCallPublisherWithTopicExchangeConsumer.RequestModelPublisherRemoteProcedureCall
                        .MessageConsumer
                        .Tell(new ConsumedMessage(consumedMessage.Message, consumedMessage.BasicDeliverEventArgs));
                }
                else
                {
                    _modelRemoteProcedureCallPublisherWithDirectQueueConsumer.RequestModelPublisherRemoteProcedureCall
                        .MessageConsumer
                        .Tell(new ConsumedMessage(consumedMessage.Message, consumedMessage.BasicDeliverEventArgs));
                }
            });
        }

        private IBasicProperties CreateProps()
        {
            var corrId = Guid.NewGuid().ToString();
            var basicProperties = _model.CreateBasicProperties();
            basicProperties.CorrelationId = corrId;

            if (_modelRemoteProcedureCallPublisherWithTopicExchangeConsumer != null)
            {
                basicProperties.ReplyToAddress = _modelRemoteProcedureCallPublisherWithTopicExchangeConsumer
                    .ConsumerPublicationAddress;
            }
            else
            {
                basicProperties.ReplyTo = _modelRemoteProcedureCallPublisherWithDirectQueueConsumer.ConsumerQueueName;
            }

            return basicProperties;
        }

        public IStash Stash { get; set; }
    }
}