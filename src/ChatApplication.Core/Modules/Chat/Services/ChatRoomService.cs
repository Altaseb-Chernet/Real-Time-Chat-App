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

    public async Task<IReadOnlyList<RoomMemberDto>> GetMembersAsync(string roomId)
    {
        var room = await _repository.GetByIdAsync(roomId)
            ?? throw new AppException(ErrorMessages.RoomNotFound, 404);

        var members = await _repository.GetMembersAsync(roomId);
        return members
            .Select(m => new RoomMemberDto(m.userId, m.username, m.joinedAt, m.userId == room.CreatedByUserId))
            .ToList();
    }

    public async Task KickMemberAsync(string roomId, string actorUserId, string targetUserId)
    {
        var room = await _repository.GetByIdAsync(roomId)
            ?? throw new AppException(ErrorMessages.RoomNotFound, 404);

        if (room.CreatedByUserId != actorUserId)
            throw new AppException(ErrorMessages.Unauthorized, 403);

        if (targetUserId == actorUserId)
            throw new AppException("You can't remove yourself. Leave the room instead.", 400);

        await _repository.RemoveMemberAsync(roomId, targetUserId);
        await _repository.SaveChangesAsync();
    }
}
