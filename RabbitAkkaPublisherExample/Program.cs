using System;
using System.Text;
using System.Threading;
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
            //Thread.Sleep(TimeSpan.FromHours(1));
            ConnectionFactory factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost",
                Port = 5672,
                VirtualHost = "/",
            };

            //const string exchangeName = "amq.topic";
            const string exchangeName = "";
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
                    if (string.IsNullOrEmpty(exchangeName))
                    {
                        requestModelPublisherRemoteProcedureCallActorRef.Tell(new PublishMessageToQueue("xxx",
                            Encoding.ASCII.GetBytes(input.Substring(1))));
                    }
                    else
                    {
                        requestModelPublisherRemoteProcedureCallActorRef.Tell(new PublishMessageUsingRoutingKey(
                            exchangeName, routingKey, Encoding.ASCII.GetBytes(input.Substring(1))));
                    }                    
                }
                else if (input != null)
                {
                    if (string.IsNullOrEmpty(exchangeName))
                    {
                        requestModelPublisherActorRef.Tell(new PublishMessageToQueue("xxx",
                            Encoding.ASCII.GetBytes(input)));
                    }
                    else
                    {
                        requestModelPublisherActorRef.Tell(new PublishMessageUsingRoutingKey(exchangeName, routingKey,
                            Encoding.ASCII.GetBytes(input)));
                    }
                }

                Console.WriteLine("type message to send, prefix with [?] for rpc (enter [q] to exit)");
                input = Console.ReadLine();                
            } while (!"q".Equals(input, StringComparison.CurrentCultureIgnoreCase));

            Task.WaitAll(actorSystem.Terminate());
        }
    }
}