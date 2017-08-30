namespace RabbitAkka.Messages.Dtos
{
    internal class ModelRemoteProcedureCallPublisherWithDirectQueueConsumer : IModelRemoteProcedureCallPublisherWithDirectQueueConsumer
    {
        public ModelRemoteProcedureCallPublisherWithDirectQueueConsumer(IRequestModelPublisherRemoteProcedureCall requestModelPublisherRemoteProcedureCall, string consumerQueueName)
        {
            RequestModelPublisherRemoteProcedureCall = requestModelPublisherRemoteProcedureCall;
            ConsumerQueueName = consumerQueueName;
        }

        public IRequestModelPublisherRemoteProcedureCall RequestModelPublisherRemoteProcedureCall { get; }
        public string ConsumerQueueName { get; }
    }
}