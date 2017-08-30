namespace RabbitAkka.Messages
{
    public interface IModelRemoteProcedureCallPublisherWithTopicExchangeConsumer
    {
        IRequestModelPublisherRemoteProcedureCall RequestModelPublisherRemoteProcedureCall { get; }
        RabbitMQ.Client.PublicationAddress ConsumerPublicationAddress { get; }
    }
}