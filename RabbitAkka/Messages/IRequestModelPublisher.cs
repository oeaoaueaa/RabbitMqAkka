namespace RabbitAkka.Messages
{
    public interface IRequestModelPublisher
    {
        bool WaitForPublishAcks { get; }
    }
}