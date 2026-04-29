using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.Chat.Models;

public class ChatRoomMember
{
    public string UserId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ChatRoom? Room { get; set; }
}
