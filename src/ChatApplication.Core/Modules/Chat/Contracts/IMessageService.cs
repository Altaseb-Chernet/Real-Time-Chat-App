using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Contracts;

public interface IMessageService
{
    Task<MessageResponse> SendMessageAsync(SendMessageRequest request);
    Task<IEnumerable<MessageResponse>> GetMessagesAsync(string roomId, int page, int pageSize);
    Task<MessageResponse> EditMessageAsync(string messageId, string userId, string newContent);
    Task DeleteMessageAsync(string messageId, string userId);
}
