using Akka.Actor;
using Akka.TestKit.NUnit;
using Moq;
using NUnit.Framework;
using RabbitAkka.Actors;
using RabbitAkka.Messages.Supervision;

namespace RabbitMqAkka.Tests
{
    public class RabbitActorSupervisorTests : TestKit
    {
        [Test]
        public void SupervisorForwardsMessagesToSupervised()
        {
            // Arrange 
            const string message = "anything";
            var supervisedTestProbe = CreateTestProbe();
            var rabbitActorSupervisor = Sys.ActorOf(RabbitActorSupervisor.CreateProps(supervisedTestProbe.Ref));

            // Act
            rabbitActorSupervisor.Tell(message);

            // Assert
            supervisedTestProbe.ExpectMsg<string>(msg => msg == message);
        }

        [Test]
        public void SupervisorPausesMessageForwarding()
        {
            // Arrange
            const string message = "anything";
            var supervisedTestProbe = CreateTestProbe();
            var rabbitActorSupervisor = Sys.ActorOf(RabbitActorSupervisor.CreateProps(supervisedTestProbe.Ref));

            // Act
            rabbitActorSupervisor.Tell(Mock.Of<IPauseProcessing>()); // pause
            rabbitActorSupervisor.Tell(message);

            // Assert
            supervisedTestProbe.ExpectNoMsg(2000);
        }

        [Test]
        public void SupervisorResumesMessageForwardingToNewDelegateAndKillsOld()
        {
            // Arrange
            const string message = "anything";
            var supervisedTestProbe1 = CreateTestProbe();
            var supervisedTestProbe2 = CreateTestProbe();
            var probe = CreateTestProbe();
            probe.Watch(supervisedTestProbe1);
            var rabbitActorSupervisor = Sys.ActorOf(RabbitActorSupervisor.CreateProps(supervisedTestProbe1.Ref));

            // Act
            rabbitActorSupervisor.Tell(Mock.Of<IPauseProcessing>()); // pause
            rabbitActorSupervisor.Tell(message);
            rabbitActorSupervisor.Tell(Mock.Of<IResumeProcessing>(rp => rp.DelegateActorRef == supervisedTestProbe2.Ref));

            // Assert
            probe.ExpectTerminated(supervisedTestProbe1);
            supervisedTestProbe1.ExpectNoMsg(2000);
            supervisedTestProbe2.ExpectMsg<string>(message);
        }
    }
}
