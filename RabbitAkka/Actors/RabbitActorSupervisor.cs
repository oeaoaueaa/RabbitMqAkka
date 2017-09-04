using Akka.Actor;
using RabbitAkka.Messages.Supervision;

namespace RabbitAkka.Actors
{
    public class RabbitActorSupervisor : ReceiveActor, IWithUnboundedStash // TODO can be made generic?
    {
        private IActorRef _supervisedActorRef;

        public IStash Stash { get; set; }

        public static Props CreateProps(IActorRef supervisedActorRef)
        {
            return Props.Create<RabbitActorSupervisor>(supervisedActorRef);
        }

        public RabbitActorSupervisor(IActorRef supervisedActorRef)
        {
            _supervisedActorRef = supervisedActorRef;

            Ready();
        }

        private void Ready()
        {
            Receive<IPauseProcessing>(pauseProcessing => Become(Paused));
            ReceiveAny(m => _supervisedActorRef.Forward(m));
        }

        private void Paused()
        {
            Receive<IResumeProcessing>(resumeProcessing =>
            {
                Context.System.Stop(_supervisedActorRef);
                _supervisedActorRef = resumeProcessing.DelegateActorRef;
                Stash.UnstashAll();
                Become(Ready);                
            });
            ReceiveAny(m => Stash.Stash()); // TODO limit number of stashed messages
        }
    }
}