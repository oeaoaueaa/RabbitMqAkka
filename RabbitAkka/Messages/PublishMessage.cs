namespace RabbitAkka.Messages
{
    public class PublishMessage
    {
        public PublishMessage(byte[] message)
        {
            Message = message;
        }

        public byte[] Message { get; }
    }
}
