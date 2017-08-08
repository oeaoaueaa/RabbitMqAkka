using System;
using Akka.Actor;
using Akka.TestKit.NUnit;
using Moq;
using NUnit.Framework;
using RabbitAkka.Actors;
using RabbitAkka.Messages;
using RabbitMQ.Client;

namespace RabbitMqAkka.Tests
{
    public class RabbitModelPublisherTests : TestKit
    {
        [Test]
        public void PublishMessageUsingRoutingKeyTest()
        {
            // Arrange 
            byte[] messageBody = { 0xBE, 0xBE };
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.BasicPublish(exchangeNameTest, routingKeyTest, It.IsAny<bool>(), It.IsAny<IBasicProperties>(), messageBody))
                .Verifiable("Message was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>();
            var rabbitModelPublisher = Sys.ActorOf(RabbitModelPublisher.CreateProps(modelMock.Object, requestModelPublisher));

            var publishMessageUsingRoutingKey = Mock.Of<IPublishMessageUsingRoutingKey>(pmurk =>
                pmurk.RoutingKey == routingKeyTest
                && pmurk.ExchangeName == exchangeNameTest
                && pmurk.Message == messageBody);

            // Act
            rabbitModelPublisher.Tell(publishMessageUsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.VerifyAll(), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
        }
    }
}
