namespace RabbitAkka.Messages.Dtos
{
    public class PublishMessageToQueue : IPublishMessageToQueue
    {
        public PublishMessageToQueue(string queueName, byte[] message)
        {
            QueueName = queueName;
            Message = message;
        }

        public string QueueName { get; }
        public byte[] Message { get; }
    }
}