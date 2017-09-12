using Akka.Actor;
using Akka.TestKit.NUnit;
using Moq;
using NUnit.Framework;
using RabbitAkka.Actors;
using RabbitAkka.Messages;
using RabbitMQ.Client;

namespace RabbitMqAkka.Tests
{
    [Timeout(3000)]
    public class RabbitConnectionTests : TestKit
    {        
        [Test]
        public async void RequestModelConsumerTest()
        {
            // Arrange 
            var connectionMock = new Mock<IConnection>();
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(connectionMock.Object);
            var rabbitConnection = Sys.ActorOf(RabbitConnection.CreateProps(connectionFactoryMock.Object));

            // Act
            var consumerActorRef = await rabbitConnection.Ask<IActorRef>(Mock.Of<IRequestModelConsumer>());

            // Assert
            Assert.IsNotNull(consumerActorRef);
        }

        [Test]
        public async void RequestModelConsumerWithConcurrencyControlTest()
        {
            // Arrange 
            var connectionMock = new Mock<IConnection>();
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(connectionMock.Object);
            var rabbitConnection = Sys.ActorOf(RabbitConnection.CreateProps(connectionFactoryMock.Object));

            // Act
            var consumerActorRef = await rabbitConnection.Ask<IActorRef>(Mock.Of<IRequestModelConsumerWithConcurrencyControl>());

            // Assert
            Assert.IsNotNull(consumerActorRef);
        }

        [Test]
        public async void RequestModelPublisherTest()
        {
            // Arrange 
            var connectionMock = new Mock<IConnection>();
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(connectionMock.Object);
            var rabbitConnection = Sys.ActorOf(RabbitConnection.CreateProps(connectionFactoryMock.Object));

            // Act
            var consumerActorRef = await rabbitConnection.Ask<IActorRef>(Mock.Of<IRequestModelPublisher>());

            // Assert
            Assert.IsNotNull(consumerActorRef);
        }

        [Test]
        public async void RequestModelPublisherRemoteProcedureCallTest()
        {
            // Arrange 
            var connectionMock = new Mock<IConnection>();
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            connectionFactoryMock.Setup(cf => cf.CreateConnection()).Returns(connectionMock.Object);
            var rabbitConnection = Sys.ActorOf(RabbitConnection.CreateProps(connectionFactoryMock.Object));

            // Act
            var consumerActorRef = await rabbitConnection.Ask<IActorRef>(Mock.Of<IRequestModelPublisherRemoteProcedureCall>());

            // Assert
            Assert.IsNotNull(consumerActorRef);
        }
    }
}
