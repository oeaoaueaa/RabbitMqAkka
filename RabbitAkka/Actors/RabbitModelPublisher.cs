using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using RabbitAkka.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitAkka.Actors
{
    public class RabbitModelPublisher : ReceiveActor
    {
        private readonly IModel _model;
        private readonly IRequestModelPublisher _requestModelPublisher;
        private IActorRef _self;
        private readonly Dictionary<ulong, MessageWaitingForAck> _messagesWaitingForAcks;
        private readonly TimeSpan _ackTimeout;

        public static Props CreateProps(IModel model, IRequestModelPublisher requestModelPublisher)
        {
            return Props.Create<RabbitModelPublisher>(model, requestModelPublisher);
        }

        public RabbitModelPublisher(IModel model, IRequestModelPublisher requestModelPublisher)
        {
            _model = model;
            _requestModelPublisher = requestModelPublisher;

            if (_requestModelPublisher.WaitForPublishAcks)
            {
                _model.ConfirmSelect();
                _model.BasicAcks +=
                    (sender, args) =>
                    {
                        _self.Tell(args, null);
                    };
            }

            _messagesWaitingForAcks = new Dictionary<ulong, MessageWaitingForAck>();

            _ackTimeout = requestModelPublisher.AckTimeout;

            Ready();
        }

        private void Ready()
        {
            Task<bool> WaitForAckIfNeeded(ulong publishSeqNo)
            {
                if (_requestModelPublisher.WaitForPublishAcks)
                {
                    var completionSource = new TaskCompletionSource<bool>();

                    var cancelable =
                        Context.System.Scheduler.ScheduleTellOnceCancelable(_ackTimeout, Self, publishSeqNo, null);
                    var messageWaitingForAck = new MessageWaitingForAck(completionSource, cancelable);
                    _messagesWaitingForAcks.Add(publishSeqNo, messageWaitingForAck);
                    return completionSource.Task;
                }
                else
                {
                    return Task.FromResult(true);
                }
            }

            Receive<IPublishMessageUsingRoutingKey>(publishMessageUsingRoutingKey =>
            {
                var publishSeqNo = _model.NextPublishSeqNo;
                _model.BasicPublish(publishMessageUsingRoutingKey.ExchangeName,
                    publishMessageUsingRoutingKey.RoutingKey, false, null, publishMessageUsingRoutingKey.Message);
                Sender.Tell(WaitForAckIfNeeded(publishSeqNo));
            });

            Receive<IPublishMessageUsingPublicationAddress>(publishMessageUsingPublicationAddress =>
            {
                var publishSeqNo = _model.NextPublishSeqNo;
                // TODO needs correlation id!
                _model.BasicPublish(publishMessageUsingPublicationAddress.PublicationAddress, null,
                    publishMessageUsingPublicationAddress.Message);
                Sender.Tell(WaitForAckIfNeeded(publishSeqNo));
            });

            Receive<IPublishMessageToQueue>(publishMessageToQueue =>
            {
                var publishSeqNo = _model.NextPublishSeqNo;
                _model.BasicPublish(string.Empty,
                    publishMessageToQueue.QueueName, false, null, publishMessageToQueue.Message);
                Sender.Tell(WaitForAckIfNeeded(publishSeqNo));
            });

            Receive<BasicAckEventArgs>(basicAckEventArgs =>
            {
                if (_messagesWaitingForAcks.TryGetValue(basicAckEventArgs.DeliveryTag,
                    out MessageWaitingForAck messageWaitingForAck))
                {
                    _messagesWaitingForAcks.Remove(basicAckEventArgs.DeliveryTag);

                    messageWaitingForAck.CancelableRetry.Cancel();
                    messageWaitingForAck.TaskCompletionSource.SetResult(true);
                }
            });

            Receive<ulong>(timedOutMessagePublishSeqNo =>
            {
                if (_messagesWaitingForAcks.TryGetValue(timedOutMessagePublishSeqNo,
                    out MessageWaitingForAck messageWaitingForAck))
                {
                    _messagesWaitingForAcks.Remove(timedOutMessagePublishSeqNo);
                    messageWaitingForAck.TaskCompletionSource.SetException(new TimeoutException(
                        $"Message with publishSeqNo={timedOutMessagePublishSeqNo} timed out after {_ackTimeout}."));
                }
            });
        }

        protected override void PreStart()
        {
            _self = Self;
        }

        class MessageWaitingForAck
        {
            public MessageWaitingForAck(TaskCompletionSource<bool> taskCompletionSource, ICancelable cancelableRetry)
            {
                TaskCompletionSource = taskCompletionSource;
                CancelableRetry = cancelableRetry;
            }

            public TaskCompletionSource<bool> TaskCompletionSource { get; }
            public ICancelable CancelableRetry { get; }
        }
    }
}