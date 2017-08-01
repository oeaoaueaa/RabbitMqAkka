using Akka.Actor;
using RabbitAkka.Messages;
using RabbitMQ.Client;

namespace RabbitAkka.Actors
{
    public class RabbitConnection : ReceiveActor
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
            Receive<RequestModelConsumer>(requestModel =>
            {
                var model = _conn.CreateModel();

                var rabbitModelActorRef = Context.System.ActorOf(RabbitModelConsumer.CreateProps(model, requestModel));

                Sender.Tell(rabbitModelActorRef);
            });
            Receive<RequestModelPublisher>(requestModelPublisher =>
            {
                var model = _conn.CreateModel();

                var rabbitModelActorRef = Context.System.ActorOf(RabbitModelPublisher.CreateProps(model, requestModelPublisher));

                Sender.Tell(rabbitModelActorRef);
            });
        }
    }
}
