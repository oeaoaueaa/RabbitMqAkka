namespace RabbitAkka.Messages.Dtos
{
    public class RequestModelPublisher : IRequestModelPublisher
    {
        public RequestModelPublisher(bool waitForPublishAcks)
        {
            WaitForPublishAcks = waitForPublishAcks;
        }

        public bool WaitForPublishAcks { get; }
    }
}
