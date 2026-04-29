using ChatApplication.Shared.Enums;

namespace ChatApplication.Core.Modules.User.Models;

public class UserStatus
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public UserStatusType Status { get; set; }
    public DateTime LastSeen { get; set; }
}
