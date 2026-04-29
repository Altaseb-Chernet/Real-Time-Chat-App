namespace ChatApplication.Infrastructure.Messaging.RabbitMQ;

public class EventHandler
{
    public Task HandleAsync<T>(T @event)
    {
        return Task.CompletedTask;
    }
}
