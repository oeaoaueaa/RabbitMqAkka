using Akka.Actor;

namespace RabbitAkka.Messages.Supervision
{
    public interface IResumeProcessing
    {
        IActorRef DelegateActorRef { get; }
    }
}