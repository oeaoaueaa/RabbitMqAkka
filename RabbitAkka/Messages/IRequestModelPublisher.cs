using System;

namespace RabbitAkka.Messages
{
    public interface IRequestModelPublisher
    {
        bool WaitForPublishAcks { get; }
        TimeSpan AckTimeout { get; }
    }
}