using ChatApplication.Core.Common.Base;
using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.User.Models;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChatRoom> CreatedRooms { get; set; } = new List<ChatRoom>();
    public ICollection<ChatRoomMember> ChatRoomMemberships { get; set; } = new List<ChatRoomMember>();
}
