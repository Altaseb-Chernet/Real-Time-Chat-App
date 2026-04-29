using ChatApplication.Infrastructure.Messaging.Interfaces;

namespace ChatApplication.Infrastructure.Messaging.Consumers;

public class NotificationConsumer
{
    private readonly IMessageSubscriber _subscriber;

    public NotificationConsumer(IMessageSubscriber subscriber) => _subscriber = subscriber;

    public Task StartAsync()
        => _subscriber.SubscribeAsync<object>("notifications", HandleAsync);

    private Task HandleAsync(object notification)
    {
        return Task.CompletedTask;
    }
}
