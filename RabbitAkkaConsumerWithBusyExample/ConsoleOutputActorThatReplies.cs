using System;
using System.Text;
using Akka.Actor;
using RabbitMQ.Client;
using RabbitAkka.Messages;
using RabbitAkka.Messages.Dtos;

namespace RabbitAkkaConsumerWithBusyExample
{
    partial class Program
    {
        class ConsoleOutputActorThatReplies : ReceiveActor
        {
            private readonly string _name;
            private readonly long _delayMs;
            private readonly IActorRef _publisherActorRef;

            public static Props CreateProps(string name, long delayMs, IActorRef publisherActorRef)
            {
                return Props.Create<ConsoleOutputActorThatReplies>(name, delayMs, publisherActorRef);
            }

            public ConsoleOutputActorThatReplies(string name, long delayMs, IActorRef publisherActorRef)
            {
                _name = name;
                _delayMs = delayMs;
                _publisherActorRef = publisherActorRef;

                Ready();
            }

            private void Ready()
            {
                Receive<IConsumedMessage>(consumedMessage =>
                {
                    var messageBody = Encoding.ASCII.GetString(consumedMessage.Message);


                    if (consumedMessage.BasicDeliverEventArgs.BasicProperties.IsReplyToPresent())
                    {
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(_delayMs), Self,
                            new WorkItemWithReply(DateTime.Now, messageBody, Sender, consumedMessage.BasicDeliverEventArgs.BasicProperties.ReplyToAddress), Self);                        
                    }
                    else
                    {
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(_delayMs), Self,
                            new WorkItem(DateTime.Now, messageBody, Sender), Self);
                    }
                });                
                Receive<WorkItemWithReply>(workItemWithReply =>
                {
                    Console.WriteLine(
                        $"{_name} {workItemWithReply.Timestamp:T} received ({workItemWithReply.Message})");
                    workItemWithReply.Sender.Tell(new MessageProcessed());

                    // Reply if needed
                    var replyBody = Encoding.ASCII.GetBytes($"Replying to {workItemWithReply.Message}");

                    _publisherActorRef.Tell(new PublishMessageUsingPublicationAddress(
                        workItemWithReply.BasicPropertiesReplyToAddress,
                        replyBody));
                });
                Receive<WorkItem>(message =>
                {
                    Console.WriteLine($"{_name} {message.Timestamp:T} received ({message.Message})");
                    message.Sender.Tell(new MessageProcessed());
                });
            }

            class WorkItemWithReply : WorkItem
            {
                public PublicationAddress BasicPropertiesReplyToAddress { get; }

                public WorkItemWithReply(DateTime timestamp, string message, IActorRef sender, PublicationAddress basicPropertiesReplyToAddress) : base(timestamp, message, sender)
                {
                    BasicPropertiesReplyToAddress = basicPropertiesReplyToAddress;
                }
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
