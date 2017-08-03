using System;
using System.Text;
using Akka.Actor;
using RabbitAkka.Messages;

namespace RabbitAkkaPublisherExample
{
    partial class Program
    {
        class ConsoleOutputActor : ReceiveActor
        {
            public static Props CreateProps()
            {
                return Props.Create<ConsoleOutputActor>();
            }

            public ConsoleOutputActor()
            {
                Ready();
            }

            private void Ready()
            {
                Receive<IConsumedMessage>(consumedMessage =>
                {
                    var messageBody = Encoding.ASCII.GetString(consumedMessage.Message);

                    Console.WriteLine($"Received response {messageBody}");
                });
               }
            
        }
    }
}