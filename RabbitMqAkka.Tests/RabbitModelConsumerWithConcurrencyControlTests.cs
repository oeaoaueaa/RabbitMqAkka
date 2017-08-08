using System.Collections.Generic;
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
    public class RabbitModelConsumerWithConcurrencyControlTests : TestKit
    {
        [Test]
        public async void RabbitModelConsumerWithConcurrencyControlTest()
        {
            // Arrange 
            byte[] messageBody1 = { 0xBE, 0xBE };
            byte[] messageBody2 = { 0xBA, 0xBA };

            var modelMock = new Mock<IModel>();

            IBasicConsumer mockConsumer = null;
            modelMock.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<EventingBasicConsumer>()))
                .Returns("consumerTag")
                .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>(
                    (queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer) =>
                    {
                        mockConsumer = consumer;
                    });

            
            var messageConsumerActorRef = CreateTestProbe("MessageConsumer");
            var requestModelConsumer = Mock.Of<IRequestModelConsumerWithConcurrencyControl>(rmc => rmc.MessageConsumer == messageConsumerActorRef 
                                                                                                && rmc.ConcurrencyLevel == 1);

            var rabbitModelConsumer = Sys.ActorOf(RabbitModelConsumerWithConcurrencyControl.CreateProps(modelMock.Object, requestModelConsumer));

            // Act
            var started = await rabbitModelConsumer.Ask<bool>("start");
            
            EventingBasicConsumer x = (EventingBasicConsumer) mockConsumer;

            // Send two messages
            x.HandleBasicDeliver("", 1, false, "", "", null, messageBody1);
            x.HandleBasicDeliver("", 1, false, "", "", null, messageBody1);

            // Assert
            Assert.IsTrue(started);

            // Receive only the first message as concurrency is set to 1
            messageConsumerActorRef.ExpectMsg<IConsumedMessage>(
                consumedMessage => consumedMessage.Message == messageBody1);

            Assert.IsFalse(messageConsumerActorRef.HasMessages);

            // Acknowledge message processed 
            messageConsumerActorRef.Send(rabbitModelConsumer, Mock.Of<IMessageProcessed>());

            x.HandleBasicDeliver("", 1, false, "", "", null, messageBody2);

            messageConsumerActorRef.ExpectMsg<IConsumedMessage>(
                consumedMessage => consumedMessage.Message == messageBody2);
        }
    }
}
