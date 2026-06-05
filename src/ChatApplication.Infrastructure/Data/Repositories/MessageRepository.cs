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
            .Where(m => m.RoomId == roomId)
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
        message.Content = "🚫 This message was deleted";
        message.MediaUrl = null;
        message.MediaType = null;
        message.MediaName = null;
        message.MediaBytes = null;
        message.MediaPublicId = null;
        
        if (message.SentAt.Kind == DateTimeKind.Unspecified)
            message.SentAt = DateTime.SpecifyKind(message.SentAt, DateTimeKind.Utc);
        if (message.EditedAt.HasValue && message.EditedAt.Value.Kind == DateTimeKind.Unspecified)
            message.EditedAt = DateTime.SpecifyKind(message.EditedAt.Value, DateTimeKind.Utc);
    }
}
