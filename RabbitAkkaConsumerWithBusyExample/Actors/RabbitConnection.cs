using Akka.Actor;
using RabbitAkkaConsumerWithBusyExample.Messages;
using RabbitMQ.Client;

namespace RabbitAkkaConsumerWithBusyExample.Actors
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
            Receive<RequestModelConsumer>(requestModel =>
            {
                var model = _conn.CreateModel();

                var rabbitModelActorRef = Context.System.ActorOf(RabbitModelConsumer.CreateProps(model, requestModel));

                Sender.Tell(rabbitModelActorRef);
            });
        }
    }
}
