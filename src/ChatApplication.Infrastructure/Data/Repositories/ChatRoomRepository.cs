using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Infrastructure.Data.Repositories;

public class ChatRoomRepository : GenericRepository<ChatRoom>, IChatRoomRepository
{
    public ChatRoomRepository(ApplicationDbContext context) : base(context) { }

    public Task<bool> IsMemberAsync(string roomId, string userId)
        => _context.ChatRoomMembers.AnyAsync(m => m.RoomId == roomId && m.UserId == userId);

    public Task<ChatRoomMember?> GetMemberAsync(string roomId, string userId)
        => _context.ChatRoomMembers.FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId);

    public async Task AddMemberAsync(string roomId, string userId)
    {
        var already = await IsMemberAsync(roomId, userId);
        if (already) return;
        await _context.ChatRoomMembers.AddAsync(new ChatRoomMember
        {
            RoomId = roomId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
    }

    public async Task RemoveMemberAsync(string roomId, string userId)
    {
        var member = await GetMemberAsync(roomId, userId);
        if (member is not null) _context.ChatRoomMembers.Remove(member);
    }
}
