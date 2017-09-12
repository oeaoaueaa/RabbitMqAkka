using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
        public async Task PublishMessageUsingRoutingKeyWithAckWaitsForAckTest()
        {
            // Arrange 
            byte[] message1Body = {0xBE, 0xBE};
            byte[] message2Body = {0xFE, 0x00};
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";
            ulong nextPublishSeqNo = 776;

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.NextPublishSeqNo).Returns(() => nextPublishSeqNo++);

            Expression<Action<IModel>> message1PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message1Body);
            modelMock.Setup(message1PublishAction)
                .Verifiable("Message 1 was not published");

            Expression<Action<IModel>> message2PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message2Body);
            modelMock.Setup(message2PublishAction)
                .Verifiable("Message 2 was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>(rmp => rmp.WaitForPublishAcks == true && rmp.AckTimeout == TimeSpan.FromSeconds(10));
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
            var message1Task = await rabbitModelPublisher.Ask<Task<bool>>(publishMessage1UsingRoutingKey);
            var message2Task = await rabbitModelPublisher.Ask<Task<bool>>(publishMessage2UsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            Assert.IsFalse(message1Task.IsCompleted);
            Assert.IsFalse(message2Task.IsCompleted);

            // Act Ack
            modelMock.Raise(m => m.BasicAcks += null, new BasicAckEventArgs{DeliveryTag = 776});

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            Assert.IsTrue(message1Task.IsCompleted);
            Assert.IsTrue(message1Task.Result);
            Assert.IsFalse(message2Task.IsCompleted);
        }

        [Test]
        public async Task PublishMessageUsingRoutingKeyWithAckWaitsForAckIgnoringOrderTest()
        {
            // Arrange 
            byte[] message1Body = { 0xBE, 0xBE };
            byte[] message2Body = { 0xFE, 0x00 };
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";
            ulong nextPublishSeqNo = 776;

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.NextPublishSeqNo).Returns(() => nextPublishSeqNo++);

            Expression<Action<IModel>> message1PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message1Body);
            modelMock.Setup(message1PublishAction)
                .Verifiable("Message 1 was not published");

            Expression<Action<IModel>> message2PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message2Body);
            modelMock.Setup(message2PublishAction)
                .Verifiable("Message 2 was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>(rmp => rmp.WaitForPublishAcks == true && rmp.AckTimeout == TimeSpan.FromSeconds(10));
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
            var message1Task = await rabbitModelPublisher.Ask<Task<bool>>(publishMessage1UsingRoutingKey);
            var message2Task = await rabbitModelPublisher.Ask<Task<bool>>(publishMessage2UsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            Assert.IsFalse(message1Task.IsCompleted);
            Assert.IsFalse(message2Task.IsCompleted);

            // Act Ack
            modelMock.Raise(m => m.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = 777 });

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            Assert.IsFalse(message1Task.IsCompleted);            
            Assert.IsTrue(message2Task.IsCompleted);
            Assert.IsTrue(message2Task.Result);
        }

        [Test]
        public async Task PublishMessageUsingRoutingKeyWithAckWaitsForAckWithCorrectSequenceTest()
        {
            // Arrange 
            byte[] message1Body = { 0xBE, 0xBE };
            byte[] message2Body = { 0xFE, 0x00 };
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";
            ulong nextPublishSeqNo = 776;

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.NextPublishSeqNo).Returns(() => nextPublishSeqNo++);

            Expression<Action<IModel>> message1PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message1Body);
            modelMock.Setup(message1PublishAction)
                .Verifiable("Message 1 was not published");

            Expression<Action<IModel>> message2PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message2Body);
            modelMock.Setup(message2PublishAction)
                .Verifiable("Message 2 was not published");

            var requestModelPublisher = Mock.Of<IRequestModelPublisher>(rmp => rmp.WaitForPublishAcks == true && rmp.AckTimeout == TimeSpan.FromSeconds(10));
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
            var message1Task = await rabbitModelPublisher.Ask<Task<bool>>(publishMessage1UsingRoutingKey);
            var message2Task = await rabbitModelPublisher.Ask<Task<bool>>(publishMessage2UsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            Assert.IsFalse(message1Task.IsCompleted);
            Assert.IsFalse(message2Task.IsCompleted);

            // Act Ack
            modelMock.Raise(m => m.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = 775 }); // we have published 766 and 777

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => modelMock.Verify(message2PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            Assert.IsFalse(message1Task.IsCompleted);
            Assert.IsFalse(message2Task.IsCompleted);
        }

        [Test]
        public async Task PublishMessageUsingRoutingKeyWithAckWaitsForAckAndTimeouts()
        {
            // Arrange 
            byte[] message1Body = { 0xBE, 0xBE };
            var routingKeyTest = "routingkeytest";
            var exchangeNameTest = "exchangenametest";
            ulong nextPublishSeqNo = 776;

            var modelMock = new Mock<IModel>();
            modelMock.Setup(m => m.NextPublishSeqNo).Returns(() => nextPublishSeqNo++);

            Expression<Action<IModel>> message1PublishAction = m => m.BasicPublish(exchangeNameTest, routingKeyTest,
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(), message1Body);
            modelMock.Setup(message1PublishAction)
                .Verifiable("Message 1 was not published");

            var requestModelPublisher =
                Mock.Of<IRequestModelPublisher>(rmp => rmp.WaitForPublishAcks == true &&
                                                       rmp.AckTimeout == TimeSpan.FromSeconds(1));
            var rabbitModelPublisher = Sys.ActorOf(RabbitModelPublisher.CreateProps(modelMock.Object, requestModelPublisher));

            var publishMessage1UsingRoutingKey = Mock.Of<IPublishMessageUsingRoutingKey>(pmurk =>
                pmurk.RoutingKey == routingKeyTest
                && pmurk.ExchangeName == exchangeNameTest
                && pmurk.Message == message1Body);

            // Act
            var message1Task = await rabbitModelPublisher.Ask<Task<bool>>(publishMessage1UsingRoutingKey);

            // Assert
            AwaitAssert(() => modelMock.Verify(message1PublishAction, Times.Once), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
            AwaitAssert(() => Assert.IsTrue(message1Task.IsFaulted), TimeSpan.FromMilliseconds(1100), TimeSpan.FromMilliseconds(100));
        }

        #endregion
    }
}
