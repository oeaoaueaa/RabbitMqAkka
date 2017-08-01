
using System;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using RabbitMQ.Client;
using RabbitAkka.Actors;
using RabbitAkka.Messages;

namespace RabbitAkkaPublisherExample
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

            var rabbitConnectionActorRef = actorSystem.ActorOf(RabbitConnection.CreateProps(factory));

            var rabbitModelPublisherActorRef =
                rabbitConnectionActorRef.Ask<IActorRef>(new RequestModelPublisher(exchangeName, routingKey)).Result;

            string input = null;
            do
            {
                if (input != null)
                {
                    rabbitModelPublisherActorRef.Tell(new PublishMessage(Encoding.ASCII.GetBytes(input)));
                }

                Console.WriteLine("type message (enter [q] to exit)");
                input = Console.ReadLine();                
            } while (!"q".Equals(input, StringComparison.CurrentCultureIgnoreCase));

            Task.WaitAll(actorSystem.Terminate());
        }
    }
}