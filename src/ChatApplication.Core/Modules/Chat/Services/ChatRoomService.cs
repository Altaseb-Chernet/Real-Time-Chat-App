using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Services;

public class ChatRoomService : IChatRoomService
{
    public Task<ChatRoom> CreateRoomAsync(string name, string createdByUserId) => throw new NotImplementedException();
    public Task<ChatRoom?> GetRoomAsync(string roomId) => throw new NotImplementedException();
    public Task<IEnumerable<ChatRoom>> GetRoomsAsync() => throw new NotImplementedException();
    public Task DeleteRoomAsync(string roomId) => throw new NotImplementedException();
}
