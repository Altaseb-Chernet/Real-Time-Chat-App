using System.Text;
using System.Text.Json;
using ChatApplication.Infrastructure.Messaging.Interfaces;

namespace ChatApplication.Infrastructure.Messaging.RabbitMQ;

public class MessagePublisher : IMessagePublisher
{
    private readonly RabbitMqConnection _connection;

    public MessagePublisher(RabbitMqConnection connection) => _connection = connection;

    public Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        using var channel = _connection.CreateChannel();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        channel.BasicPublish(exchange, routingKey, null, body);
        return Task.CompletedTask;
    }
}
