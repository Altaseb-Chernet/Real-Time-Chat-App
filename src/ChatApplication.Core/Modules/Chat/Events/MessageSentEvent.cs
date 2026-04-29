namespace ChatApplication.Core.Modules.Chat.Events;

public class MessageSentEvent
{
    public string MessageId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
