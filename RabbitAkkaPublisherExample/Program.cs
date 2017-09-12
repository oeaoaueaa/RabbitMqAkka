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
                rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelPublisher(true, TimeSpan.FromSeconds(10))).Result;

            var consoleOutputActorRef = actorSystem.ActorOf(ConsoleOutputActor.CreateProps());            

            var requestModelPublisherRemoteProcedureCallActorRef =
                rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelPublisherRemoteProcedureCall(exchangeName, routingKey, consoleOutputActorRef)).Result;

            string input = null;
            do
            {
                Task<bool> publishTask = null;
                if (input?.StartsWith("?", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    if (string.IsNullOrEmpty(exchangeName))
                    {
                        publishTask = requestModelPublisherRemoteProcedureCallActorRef.Ask<Task<bool>>(new PublishMessageToQueue(
                            "xxx",
                            Encoding.ASCII.GetBytes(input.Substring(1)))).Result;
                    }
                    else
                    {
                        publishTask = requestModelPublisherRemoteProcedureCallActorRef.Ask<Task<bool>>(new PublishMessageUsingRoutingKey(
                            exchangeName, routingKey, Encoding.ASCII.GetBytes(input.Substring(1)))).Result;
                    }                    
                }
                else if (input != null)
                {
                    if (string.IsNullOrEmpty(exchangeName))
                    {
                        publishTask = requestModelPublisherActorRef.Ask<Task<bool>>(new PublishMessageToQueue("xxx",
                            Encoding.ASCII.GetBytes(input))).Result;
                    }
                    else
                    {
                        publishTask = requestModelPublisherActorRef.Ask<Task<bool>>(new PublishMessageUsingRoutingKey(exchangeName, routingKey,
                            Encoding.ASCII.GetBytes(input))).Result;
                    }
                }

                if (publishTask != null)
                {
                    var messageInput = input;
                    publishTask.ContinueWith(_ => Console.WriteLine($"Published {messageInput}"),
                        TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
                    publishTask.ContinueWith(_ => Console.WriteLine($"Could not publish {messageInput}"),
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.AttachedToParent);
                }
                Console.WriteLine("type message to send, prefix with [?] for rpc (enter [q] to exit)");
                input = Console.ReadLine();                
            } while (!"q".Equals(input, StringComparison.CurrentCultureIgnoreCase));

            Task.WaitAll(actorSystem.Terminate());
        }
    }
}