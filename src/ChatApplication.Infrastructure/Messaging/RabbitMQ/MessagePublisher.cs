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
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        channel.BasicPublish(exchange, routingKey, mandatory: false, basicProperties: props, body: body);
        return Task.CompletedTask;
    }
}
