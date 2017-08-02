﻿using Akka.Actor;

namespace RabbitAkka.Messages
{
    public class RequestModelConsumer
    {
        public RequestModelConsumer(string exchangeName, string queueName, string routingKey, IActorRef messageConsumer)
        {
            ExchangeName = exchangeName;
            QueueName = queueName;
            RoutingKey = routingKey;
            MessageConsumer = messageConsumer;
        }

        public string ExchangeName { get; }
        public string QueueName { get; }
        public string RoutingKey { get; }
        public IActorRef MessageConsumer { get; }
    }
}