using System;
using Akka.Actor;
using RabbitAkka.Messages.Supervision;

namespace RabbitAkka.Actors
{
    // TODO can be made generic?
    public class RabbitActorSupervisor : ReceiveActor, IWithUnboundedStash 
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
            Receive<IPauseProcessing>(pauseProcessing =>
            {
                Become(Paused);
            });
            ReceiveAny(m => _supervisedActorRef.Forward(m));
        }

        private void Paused()
        {
            Receive<IResumeProcessingWithNewActor>(resumeProcessing =>
            {
                Context.System.Stop(_supervisedActorRef);
                _supervisedActorRef = resumeProcessing.DelegateActorRef;
                Become(Ready);
                Stash.UnstashAll();
            });
            Receive<IResumeProcessing>(resumeProcessing =>
            {
                Become(Ready);
                Stash.UnstashAll();
            });
            ReceiveAny(m =>
            {
                // TODO limit number of stashed messages to avoid overflows
                Stash.Stash();
            }); 
        }
    }
}