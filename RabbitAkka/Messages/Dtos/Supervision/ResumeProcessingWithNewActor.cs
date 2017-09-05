using Akka.Actor;
using RabbitAkka.Messages.Supervision;

namespace RabbitAkka.Messages.Dtos.Supervision
{
    public class ResumeProcessingWithNewActor : IResumeProcessingWithNewActor
    {
        public ResumeProcessingWithNewActor(IActorRef delegateActorRef)
        {
            DelegateActorRef = delegateActorRef;
        }

        public IActorRef DelegateActorRef { get; }
    }
}