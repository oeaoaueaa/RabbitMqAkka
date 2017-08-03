using RabbitMQ.Client;

namespace RabbitAkka.Messages.Dtos
{
    public class PublishMessageUsingPublicationAddress : IPublishMessageUsingPublicationAddress
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