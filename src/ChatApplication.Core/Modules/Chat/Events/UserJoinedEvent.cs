namespace ChatApplication.Core.Modules.Chat.Events;

public class UserJoinedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
