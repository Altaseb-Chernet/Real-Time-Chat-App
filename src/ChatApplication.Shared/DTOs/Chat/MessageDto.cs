namespace ChatApplication.Shared.DTOs.Chat;

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
