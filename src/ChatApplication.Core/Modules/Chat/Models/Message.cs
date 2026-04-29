using ChatApplication.Core.Common.Base;

namespace ChatApplication.Core.Modules.Chat.Models;

public class Message : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public User? Sender { get; set; }
    public ChatRoom? Room { get; set; }
}
