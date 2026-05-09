using ChatApplication.Core.Common.Base;

namespace ChatApplication.Core.Modules.Chat.Models;

public class Message : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    // Optional media attachment (uploaded via /api/media/upload).
    public string? MediaUrl { get; set; }
    public string? MediaPublicId { get; set; }
    public string? MediaType { get; set; }
    public string? MediaName { get; set; }
    public long? MediaBytes { get; set; }

    public bool IsDeleted { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }

    // Navigation properties
    public AppUser? Sender { get; set; }
    public ChatRoom? Room { get; set; }
}
