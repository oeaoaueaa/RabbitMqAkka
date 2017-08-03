using RabbitMQ.Client.Events;

namespace RabbitAkka.Messages.Dtos
{
    public class ConsumedMessage : IConsumedMessage
    {
        public BasicDeliverEventArgs BasicDeliverEventArgs { get; }

        public byte[] Message { get; }

        public ConsumedMessage(byte[] message, BasicDeliverEventArgs basicDeliverEventArgs) 
        {
            Message = message;
            BasicDeliverEventArgs = basicDeliverEventArgs;
        }
    }
}