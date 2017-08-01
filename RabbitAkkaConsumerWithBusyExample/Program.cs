using System;
using System.Text;
using Akka.Actor;
using RabbitMQ.Client;
using RabbitAkka.Actors;
using RabbitAkka.Messages;

namespace RabbitAkkaConsumerWithBusyExample
{
    class Program
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

            const string exchangeName = "jc";
            const string routingKey = "routingKey";


            var actorSystem = ActorSystem.Create("RabbitAkkaExample");


            var consoleOutputOne = actorSystem.ActorOf(ConsoleOutputActor.CreateProps("One", 3000));
            var consoleOutputTwo = actorSystem.ActorOf(ConsoleOutputActor.CreateProps("Two", 6000));

            var rabbitConnectionActorRef = actorSystem.ActorOf(RabbitConnection.CreateProps(factory));

            var rabbitModelOne = rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelConsumer(
                exchangeName,
                "xxx",//"one",
                routingKey,
                1,
                consoleOutputOne)).Result;

            var rabbitModelTwo = rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelConsumer(
                exchangeName,
                "xxx", //"two",
                routingKey,
                3,
                consoleOutputTwo)).Result;

            rabbitModelOne.Tell("start");
            rabbitModelTwo.Tell("start");

            Console.ReadLine();
        }


        class ConsoleOutputActor : ReceiveActor
        {
            private readonly string _name;
            private readonly long _delayMs;

            public static Props CreateProps(string name, long delayMs)
            {
                return Props.Create<ConsoleOutputActor>(name, delayMs);
            }

            public ConsoleOutputActor(string name, long delayMs)
            {
                _name = name;
                _delayMs = delayMs;

                Ready();
            }

            private void Ready()
            {
                Receive<byte[]>(messageBody =>
                {
                    Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(_delayMs), Self,
                        new WorkItem(DateTime.Now, Encoding.ASCII.GetString(messageBody), Sender), Self);
                });
                Receive<WorkItem>(message =>
                {
                    Console.WriteLine($"{_name} {message.Timestamp:T} received ({message.Message})");
                    message.Sender.Tell(new MessageProcessed());
                });
            }

            class WorkItem
            {
                public WorkItem(DateTime timestamp, string message, IActorRef sender)
                {
                    Timestamp = timestamp;
                    Message = message;
                    Sender = sender;
                }

                public DateTime Timestamp { get; }
                public string Message { get; }
                public IActorRef Sender { get; }
            }
        }
    }
}
