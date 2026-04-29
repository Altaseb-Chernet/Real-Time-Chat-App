using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Infrastructure.Data.Repositories;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context) { }

    public Task<List<Message>> GetByRoomAsync(string roomId, int skip, int take)
        => _dbSet
            .Where(m => m.RoomId == roomId && !m.IsDeleted)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip).Take(take)
            .ToListAsync();

    public async Task<Message?> GetByIdWithSenderAsync(string id)
        => await _dbSet
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task SoftDeleteAsync(string messageId)
    {
        var message = await _dbSet.FindAsync(messageId);
        if (message is null) return;
        message.IsDeleted = true;
        _dbSet.Update(message);
    }
}
