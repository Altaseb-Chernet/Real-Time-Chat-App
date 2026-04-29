using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Infrastructure.Data.Context;

namespace ChatApplication.Infrastructure.Data.Repositories;

public class ChatRoomRepository : GenericRepository<ChatRoom>
{
    public ChatRoomRepository(ApplicationDbContext context) : base(context) { }
}
