using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Services;

public class MessageProcessor : IMessageProcessor
{
    public Task<Message> ProcessAsync(SendMessageRequest request) => throw new NotImplementedException();
}
