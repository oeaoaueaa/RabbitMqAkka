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
    public class RabbitModelConsumerTests : TestKit
    {
        [Test]
        public async void RequestModelConsumerTest()
        {
            // Arrange 
            byte[] messageBody = { 0xBE, 0xBE };

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
            var requestModelConsumer = Mock.Of<IRequestModelConsumer>(rmc => rmc.MessageConsumer == messageConsumerActorRef);

            var rabbitModelConsumer = Sys.ActorOf(RabbitModelConsumer.CreateProps(modelMock.Object, requestModelConsumer));

            // Act
            var started = await rabbitModelConsumer.Ask<bool>("start");
            
            EventingBasicConsumer x = (EventingBasicConsumer) mockConsumer;
            x.HandleBasicDeliver("", 1, false, "", "", null, messageBody);

            
            // Assert
            Assert.IsTrue(started);
            messageConsumerActorRef.ExpectMsg<IConsumedMessage>(
                consumedMessage => consumedMessage.Message == messageBody);

        }
    }
}
