using RabbitMQ.Client;

namespace RabbitAkka.Messages.Dtos
{
    internal class ModelRemoteProcedureCallPublisherWithTopicExchangeConsumer : IModelRemoteProcedureCallPublisherWithTopicExchangeConsumer
    {
        public ModelRemoteProcedureCallPublisherWithTopicExchangeConsumer(IRequestModelPublisherRemoteProcedureCall requestModelPublisherRemoteProcedureCall, PublicationAddress consumerPublicationAddress)
        {
            RequestModelPublisherRemoteProcedureCall = requestModelPublisherRemoteProcedureCall;
            ConsumerPublicationAddress = consumerPublicationAddress;
        }

        public IRequestModelPublisherRemoteProcedureCall RequestModelPublisherRemoteProcedureCall { get; }
        public PublicationAddress ConsumerPublicationAddress { get; }
    }
}