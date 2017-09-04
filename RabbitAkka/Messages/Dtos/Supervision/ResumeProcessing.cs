using Akka.Actor;
using RabbitAkka.Messages.Supervision;

namespace RabbitAkka.Messages.Dtos.Supervision
{
    public class ResumeProcessing : IResumeProcessing
    {
        public ResumeProcessing(IActorRef delegateActorRef)
        {
            DelegateActorRef = delegateActorRef;
        }

        public IActorRef DelegateActorRef { get; }
    }
}