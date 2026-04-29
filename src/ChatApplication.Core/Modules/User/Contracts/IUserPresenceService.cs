using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.User.Contracts;

public interface IUserPresenceService
{
    Task SetOnlineAsync(string userId);
    Task SetOfflineAsync(string userId);
    Task<UserStatus> GetStatusAsync(string userId);
}
