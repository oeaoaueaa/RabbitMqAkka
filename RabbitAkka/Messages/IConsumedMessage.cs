using RabbitMQ.Client.Events;

namespace RabbitAkka.Messages
{
    public interface IConsumedMessage
    {
        BasicDeliverEventArgs BasicDeliverEventArgs { get; }
        byte[] Message { get; }
    }
}