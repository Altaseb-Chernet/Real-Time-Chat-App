using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.User.Contracts;

public interface IUserPresenceService
{
    Task SetOnlineAsync(string userId, string username);
    Task SetOfflineAsync(string userId);
    Task SetAwayAsync(string userId);
    Task<UserStatus> GetStatusAsync(string userId);
    Task<IEnumerable<UserStatus>> GetOnlineUsersAsync();
    Task<bool> IsOnlineAsync(string userId);
}
