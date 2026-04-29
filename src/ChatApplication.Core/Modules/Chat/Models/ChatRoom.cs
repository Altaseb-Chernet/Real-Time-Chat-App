using ChatApplication.Core.Common.Base;

namespace ChatApplication.Core.Modules.Chat.Models;

public class ChatRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
