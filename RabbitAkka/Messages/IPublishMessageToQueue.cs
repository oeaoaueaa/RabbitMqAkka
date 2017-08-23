namespace RabbitAkka.Messages
{
    public interface IPublishMessageToQueue
    {
        string QueueName { get; }
        byte[] Message { get; }        
    }
}