using RabbitMQ.Client;

namespace RabbitAkka.Messages
{
    public class PublishMessageUsingPublicationAddress
    {
        public PublishMessageUsingPublicationAddress(PublicationAddress publicationAddress, byte[] message)
        {
            PublicationAddress = publicationAddress;
            Message = message;
        }

        public PublicationAddress PublicationAddress { get; }
        public byte[] Message { get; }        
    }
}