using ChatApplication.Core.Common.Base;

namespace ChatApplication.Core.Modules.Chat.Models;

public class PrivateMessage : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsDeleted { get; set; }

    // Optional media attachment (uploaded via /api/media/upload).
    public string? MediaUrl { get; set; }
    public string? MediaPublicId { get; set; }
    public string? MediaType { get; set; }
    public string? MediaName { get; set; }
    public long? MediaBytes { get; set; }

    // Navigation properties
    public AppUser? Sender { get; set; }
    public AppUser? Recipient { get; set; }
}
