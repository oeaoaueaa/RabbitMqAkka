using System;

namespace RabbitAkka.Messages.Dtos
{
    public class RequestModelPublisher : IRequestModelPublisher
    {
        public RequestModelPublisher(bool waitForPublishAcks, TimeSpan ackTimeout)
        {
            WaitForPublishAcks = waitForPublishAcks;
            AckTimeout = ackTimeout;
        }

        public bool WaitForPublishAcks { get; }
        public TimeSpan AckTimeout { get; }
    }
}
