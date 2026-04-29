using ChatApplication.Shared.Enums;

namespace ChatApplication.Shared.DTOs.User;

public class UserPresenceDto
{
    public string UserId { get; set; } = string.Empty;
    public UserStatusType Status { get; set; }
    public DateTime LastSeen { get; set; }
}
