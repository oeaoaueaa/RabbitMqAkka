using System;
using System.Linq.Expressions;
using Akka.Actor;
using Akka.TestKit.NUnit;
using Moq;
using NUnit.Framework;
using RabbitAkka.Actors;
using RabbitAkka.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMqAkka.Tests
{
    public class RabbitModelPublisherTests : TestKit
    {
        #region No Ack

        [Test]
        public void PublishMessageUsingRoutingKeyNoAckTest()
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

        [Test]
        public void PublishMessageUsingPublicationAddressNoAckTest()
        {
            // Arrange 
            byte[] messageBody = { 0xBE, 0xBE };
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";
            var publicationAddressTest = new PublicationAddress(ExchangeType.Topic, exchangeNameTest, routingKeyTest);

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.BasicPublish(exchangeNameTest, routingKeyTest, It.IsAny<bool>(), It.IsAny<IBasicProperties>(), messageBody))
                .Verifiable("Message was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>();
            var rabbitModelPublisher = Sys.ActorOf(RabbitModelPublisher.CreateProps(modelMock.Object, requestModelPublisher));

            var publishMessageUsingRoutingKey = Mock.Of<IPublishMessageUsingPublicationAddress>(pmurk =>
                pmurk.PublicationAddress == publicationAddressTest
                && pmurk.Message == messageBody);

            // Act
            rabbitModelPublisher.Tell(publishMessageUsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.VerifyAll(), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
        }

        [Test]
        public void PublishMessageToQueueNoAckTest()
        {
            // Arrange 
            byte[] messageBody = { 0xBE, 0xBE };
            var queueNameTest = "queueNameTest";
            

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.BasicPublish(string.Empty, queueNameTest, It.IsAny<bool>(), It.IsAny<IBasicProperties>(), messageBody))
                .Verifiable("Message was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>();
            var rabbitModelPublisher = Sys.ActorOf(RabbitModelPublisher.CreateProps(modelMock.Object, requestModelPublisher));

            var publishMessageUsingRoutingKey = Mock.Of<IPublishMessageToQueue>(pmtq =>
                pmtq.QueueName == queueNameTest
                && pmtq.Message == messageBody);

            // Act
            rabbitModelPublisher.Tell(publishMessageUsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.VerifyAll(), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
        }

        #endregion

        #region With Ack

        [Test]
        public void PublishMessageUsingRoutingKeyWithAckWaitsForAckTest()
        {
            // Arrange 
            byte[] message1Body = {0xBE, 0xBE};
            byte[] message2Body = {0xFE, 0x00};
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.NextPublishSeqNo).Returns(777);

            Expression<Action<IModel>> message1PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message1Body);
            modelMock.Setup(message1PublishAction)
                .Verifiable("Message 1 was not published");

            Expression<Action<IModel>> message2PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message2Body);
            modelMock.Setup(message2PublishAction)
                .Verifiable("Message 2 was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>(rmp => rmp.WaitForPublishAcks == true);
            var rabbitModelPublisher = Sys.ActorOf(RabbitModelPublisher.CreateProps(modelMock.Object, requestModelPublisher));

            var publishMessage1UsingRoutingKey = Mock.Of<IPublishMessageUsingRoutingKey>(pmurk =>
                pmurk.RoutingKey == routingKeyTest
                && pmurk.ExchangeName == exchangeNameTest
                && pmurk.Message == message1Body);

            var publishMessage2UsingRoutingKey = Mock.Of<IPublishMessageUsingRoutingKey>(pmurk =>
                pmurk.RoutingKey == routingKeyTest
                && pmurk.ExchangeName == exchangeNameTest
                && pmurk.Message == message2Body);

            // Act
            rabbitModelPublisher.Tell(publishMessage1UsingRoutingKey);
            rabbitModelPublisher.Tell(publishMessage2UsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Never), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));

            // Act Ack
            modelMock.Raise(m => m.BasicAcks += null, new BasicAckEventArgs{DeliveryTag = 776});

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
        }

        [Test]
        public void PublishMessageUsingRoutingKeyWithAckWaitsForAckWithCorrectSequenceTest()
        {
            // Arrange 
            byte[] message1Body = { 0xBE, 0xBE };
            byte[] message2Body = { 0xFE, 0x00 };
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.NextPublishSeqNo).Returns(777);

            Expression<Action<IModel>> message1PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message1Body);
            modelMock.Setup(message1PublishAction)
                .Verifiable("Message 1 was not published");

            Expression<Action<IModel>> message2PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message2Body);
            modelMock.Setup(message2PublishAction)
                .Verifiable("Message 2 was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>(rmp => rmp.WaitForPublishAcks == true);
            var rabbitModelPublisher = Sys.ActorOf(RabbitModelPublisher.CreateProps(modelMock.Object, requestModelPublisher));

            var publishMessage1UsingRoutingKey = Mock.Of<IPublishMessageUsingRoutingKey>(pmurk =>
                pmurk.RoutingKey == routingKeyTest
                && pmurk.ExchangeName == exchangeNameTest
                && pmurk.Message == message1Body);

            var publishMessage2UsingRoutingKey = Mock.Of<IPublishMessageUsingRoutingKey>(pmurk =>
                pmurk.RoutingKey == routingKeyTest
                && pmurk.ExchangeName == exchangeNameTest
                && pmurk.Message == message2Body);

            // Act
            rabbitModelPublisher.Tell(publishMessage1UsingRoutingKey);
            rabbitModelPublisher.Tell(publishMessage2UsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Never), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));

            // Act Ack
            modelMock.Raise(m => m.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = 775 }); // should equals or bigger than 776

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Never), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
        }

        #endregion
    }
}
