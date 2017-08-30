namespace RabbitAkka.Messages
{
    public interface IModelRemoteProcedureCallPublisherWithDirectQueueConsumer
    {
        IRequestModelPublisherRemoteProcedureCall RequestModelPublisherRemoteProcedureCall { get; }
        string ConsumerQueueName { get; }
    }
}