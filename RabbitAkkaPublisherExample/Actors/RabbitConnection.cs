using Akka.Actor;
using RabbitAkkaPublisherExample.Messages;
using RabbitMQ.Client;

namespace RabbitAkkaPublisherExample.Actors
{
    class RabbitConnection : ReceiveActor
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _conn;

        public static Props CreateProps(IConnectionFactory connectionFactory)
        {
            return Props.Create<RabbitConnection>(connectionFactory);
        }

        public RabbitConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            Ready();
        }

        private void Ready()
        {
            _conn = _connectionFactory.CreateConnection();
            Receive<RequestModelPublisher>(requestModelPublisher =>
            {
                var model = _conn.CreateModel();

                var rabbitModelActorRef = Context.System.ActorOf(RabbitModelPublisher.CreateProps(model, requestModelPublisher));

                Sender.Tell(rabbitModelActorRef);
            });
        }
    }
}
