namespace ChatApplication.Core.Modules.Chat.Models;

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
}
