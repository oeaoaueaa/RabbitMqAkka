using RabbitMQ.Client.Events;

namespace RabbitAkka.Messages
{
    public class ConsumedMessage
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