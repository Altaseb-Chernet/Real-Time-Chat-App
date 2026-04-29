using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Contracts;

public interface IChatRoomRepository
{
    Task<ChatRoom?> GetByIdAsync(string id);
    Task<IEnumerable<ChatRoom>> GetAllAsync();
    Task<ChatRoom> AddAsync(ChatRoom room);
    Task DeleteAsync(string id);
    Task<bool> IsMemberAsync(string roomId, string userId);
    Task AddMemberAsync(string roomId, string userId);
    Task RemoveMemberAsync(string roomId, string userId);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
