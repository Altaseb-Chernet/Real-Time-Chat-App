namespace ChatApplication.Core.Modules.User.Events;

public class UserConnectedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
