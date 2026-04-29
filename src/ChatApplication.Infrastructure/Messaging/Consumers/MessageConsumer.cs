using ChatApplication.Core.Modules.Chat.Events;
using ChatApplication.Infrastructure.Messaging.Interfaces;

namespace ChatApplication.Infrastructure.Messaging.Consumers;

public class MessageConsumer
{
    private readonly IMessageSubscriber _subscriber;

    public MessageConsumer(IMessageSubscriber subscriber) => _subscriber = subscriber;

    public Task StartAsync()
        => _subscriber.SubscribeAsync<MessageSentEvent>("chat.messages", HandleAsync);

    private Task HandleAsync(MessageSentEvent @event)
    {
        return Task.CompletedTask;
    }
}
