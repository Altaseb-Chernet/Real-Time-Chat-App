using ChatApplication.Core.Common.Base;

namespace ChatApplication.Core.Modules.Chat.Models;

public class PrivateMessage : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public User? Sender { get; set; }
    public User? Recipient { get; set; }
}
