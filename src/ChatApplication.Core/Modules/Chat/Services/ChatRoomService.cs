using ChatApplication.Core.Common.Exceptions;
using ChatApplication.Core.Dependencies.Constants;
using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Services;

public class ChatRoomService : IChatRoomService
{
    private readonly IChatRoomRepository _repository;

    public ChatRoomService(IChatRoomRepository repository) => _repository = repository;

    public async Task<ChatRoom> CreateRoomAsync(string name, string createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new AppException("Room name is required.");

        var room = new ChatRoom
        {
            Name = name,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(room);
        await _repository.AddMemberAsync(room.Id, createdByUserId);
        await _repository.SaveChangesAsync();

        return room;
    }

    public async Task<ChatRoom?> GetRoomAsync(string roomId)
        => await _repository.GetByIdAsync(roomId);

    public async Task<IEnumerable<ChatRoom>> GetRoomsAsync()
        => await _repository.GetAllAsync();

    public async Task DeleteRoomAsync(string roomId)
    {
        var room = await _repository.GetByIdAsync(roomId)
            ?? throw new AppException(ErrorMessages.RoomNotFound, 404);

        await _repository.DeleteAsync(room.Id);
        await _repository.SaveChangesAsync();
    }

    public async Task JoinRoomAsync(string roomId, string userId)
    {
        var room = await _repository.GetByIdAsync(roomId)
            ?? throw new AppException(ErrorMessages.RoomNotFound, 404);

        await _repository.AddMemberAsync(room.Id, userId);
        await _repository.SaveChangesAsync();
    }

    public async Task LeaveRoomAsync(string roomId, string userId)
    {
        var room = await _repository.GetByIdAsync(roomId)
            ?? throw new AppException(ErrorMessages.RoomNotFound, 404);

        await _repository.RemoveMemberAsync(room.Id, userId);
        await _repository.SaveChangesAsync();
    }

    public Task<bool> IsMemberAsync(string roomId, string userId)
        => _repository.IsMemberAsync(roomId, userId);
}
