using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Services;

public class MessageService : IMessageService
{
    public Task<MessageResponse> SendMessageAsync(SendMessageRequest request) => throw new NotImplementedException();
    public Task<IEnumerable<MessageResponse>> GetMessagesAsync(string roomId, int page, int pageSize) => throw new NotImplementedException();
    public Task DeleteMessageAsync(string messageId, string userId) => throw new NotImplementedException();
}
