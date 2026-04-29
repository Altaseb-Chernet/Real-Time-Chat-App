using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Contracts;

public interface IChatRoomService
{
    Task<ChatRoom> CreateRoomAsync(string name, string createdByUserId);
    Task<ChatRoom?> GetRoomAsync(string roomId);
    Task<IEnumerable<ChatRoom>> GetRoomsAsync();
    Task DeleteRoomAsync(string roomId);
    Task JoinRoomAsync(string roomId, string userId);
    Task LeaveRoomAsync(string roomId, string userId);
    Task<bool> IsMemberAsync(string roomId, string userId);
}
