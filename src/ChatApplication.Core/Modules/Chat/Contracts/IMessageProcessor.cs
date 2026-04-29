using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Contracts;

public interface IMessageProcessor
{
    Task<Message> ProcessAsync(SendMessageRequest request);
}
