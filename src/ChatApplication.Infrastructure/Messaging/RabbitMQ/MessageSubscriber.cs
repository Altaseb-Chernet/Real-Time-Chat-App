using System.Text;
using System.Text.Json;
using ChatApplication.Infrastructure.Messaging.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatApplication.Infrastructure.Messaging.RabbitMQ;

public class MessageSubscriber : IMessageSubscriber
{
    private readonly RabbitMqConnection _connection;

    public MessageSubscriber(RabbitMqConnection connection) => _connection = connection;

    public Task SubscribeAsync<T>(string queue, Func<T, Task> handler)
    {
        var channel = _connection.CreateChannel();
        if (channel is null) return Task.CompletedTask; // RabbitMQ unavailable
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<T>(body);
            if (message != null) await handler(message);
            channel.BasicAck(ea.DeliveryTag, false);
        };
        channel.BasicConsume(queue, false, consumer);
        return Task.CompletedTask;
    }
}
