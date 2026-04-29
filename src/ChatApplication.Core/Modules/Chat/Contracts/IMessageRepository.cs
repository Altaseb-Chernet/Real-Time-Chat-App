using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Contracts;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(string id);
    Task<Message?> GetByIdWithSenderAsync(string id);
    Task<List<Message>> GetByRoomAsync(string roomId, int skip, int take);
    Task<Message> AddAsync(Message message);
    Task SoftDeleteAsync(string messageId);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
