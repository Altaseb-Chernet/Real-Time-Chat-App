using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Infrastructure.Data.Repositories;

public class MessageRepository : GenericRepository<Message>
{
    public MessageRepository(ApplicationDbContext context) : base(context) { }

    public Task<List<Message>> GetByRoomAsync(string roomId, int skip, int take)
        => _dbSet.Where(m => m.RoomId == roomId)
                 .OrderByDescending(m => m.SentAt)
                 .Skip(skip).Take(take)
                 .ToListAsync();
}
