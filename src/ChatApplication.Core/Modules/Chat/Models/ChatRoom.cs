using ChatApplication.Core.Common.Base;
using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.Chat.Models;

public class ChatRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? CreatedBy { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChatRoomMember> Members { get; set; } = new List<ChatRoomMember>();
}
