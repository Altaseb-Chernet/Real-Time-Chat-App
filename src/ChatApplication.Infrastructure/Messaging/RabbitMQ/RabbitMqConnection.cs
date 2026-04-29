using RabbitMQ.Client;

namespace ChatApplication.Infrastructure.Messaging.RabbitMQ;

public class RabbitMqConnection : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnection(IConnection connection) => _connection = connection;

    public IModel CreateChannel() => _connection.CreateModel();

    public void Dispose() => _connection.Dispose();
}
