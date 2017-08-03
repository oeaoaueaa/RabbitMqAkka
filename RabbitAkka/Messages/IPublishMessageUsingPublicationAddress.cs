using RabbitMQ.Client;

namespace RabbitAkka.Messages
{
    public interface IPublishMessageUsingPublicationAddress
    {
        byte[] Message { get; }
        PublicationAddress PublicationAddress { get; }
    }
}