namespace ChatApplication.Core.Modules.User.Models;

public class UserProfile
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
}
