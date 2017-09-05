using Akka.Actor;

namespace RabbitAkka.Messages.Supervision
{
    public interface IResumeProcessingWithNewActor
    {
        IActorRef DelegateActorRef { get; }
    }
}