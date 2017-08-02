using System;
using Akka.Actor;
using RabbitMQ.Client;
using RabbitAkka.Actors;
using RabbitAkka.Messages;

namespace RabbitAkkaConsumerWithBusyExample
{
    partial class Program
    {
        static void Main(string[] args)
        {

            ConnectionFactory factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost",
                Port = 5672,
                VirtualHost = "/",
            };

            const string exchangeName = "amq.topic";
            const string routingKey = "routingKey";


            var actorSystem = ActorSystem.Create("RabbitAkkaExample");

            var rabbitConnectionActorRef = actorSystem.ActorOf(RabbitConnection.CreateProps(factory));

            var rabbitPublisher = rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelPublisher()).Result;

            var consoleOutputOne = actorSystem.ActorOf(ConsoleOutputActorThatReplies.CreateProps("One", 3000, rabbitPublisher));
            var consoleOutputTwo = actorSystem.ActorOf(ConsoleOutputActorThatReplies.CreateProps("Two", 6000, rabbitPublisher));

            var rabbitModelOne = rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelConsumerWithConcurrencyControl(
                exchangeName,
                "xxx",//"one",
                routingKey,
                1,
                consoleOutputOne)).Result;

            var rabbitModelTwo = rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelConsumerWithConcurrencyControl(
                exchangeName,
                "xxx", //"two",
                routingKey,
                3,
                consoleOutputTwo)).Result;

            rabbitModelOne.Tell("start");
            rabbitModelTwo.Tell("start");

            Console.ReadLine();
        }
    }
}
