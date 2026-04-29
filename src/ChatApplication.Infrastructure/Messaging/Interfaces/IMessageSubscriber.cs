namespace ChatApplication.Infrastructure.Messaging.Interfaces;

public interface IMessageSubscriber
{
    Task SubscribeAsync<T>(string queue, Func<T, Task> handler);
}
