using System;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using RabbitMQ.Client;
using RabbitAkka.Actors;
using RabbitAkka.Messages.Dtos;

namespace RabbitAkkaPublisherExample
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

            var requestModelPublisherActorRef =
                rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelPublisher()).Result;

            var consoleOutputActorRef = actorSystem.ActorOf(ConsoleOutputActor.CreateProps());

            var requestModelPublisherRemoteProcedureCallActorRef =
                rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelPublisherRemoteProcedureCall(exchangeName, routingKey, consoleOutputActorRef)).Result;

            string input = null;
            do
            {
                if (input?.StartsWith("?", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    requestModelPublisherRemoteProcedureCallActorRef.Tell(new PublishMessageUsingRoutingKey(exchangeName, routingKey, Encoding.ASCII.GetBytes(input.Substring(1))));
                }
                else if (input != null)
                {
                    requestModelPublisherActorRef.Tell(new PublishMessageUsingRoutingKey(exchangeName, routingKey, Encoding.ASCII.GetBytes(input)));
                }

                Console.WriteLine("type message to send, prefix with [?] for rpc (enter [q] to exit)");
                input = Console.ReadLine();                
            } while (!"q".Equals(input, StringComparison.CurrentCultureIgnoreCase));

            Task.WaitAll(actorSystem.Terminate());
        }
    }
}