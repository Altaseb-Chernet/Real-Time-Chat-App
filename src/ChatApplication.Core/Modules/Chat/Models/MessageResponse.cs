namespace ChatApplication.Core.Modules.Chat.Models;

public class MessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
