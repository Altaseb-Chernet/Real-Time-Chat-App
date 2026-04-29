using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.User.Services;

// Stub — real implementation is RedisUserPresenceService in Infrastructure.
// Kept here so the Core project compiles; never registered in DI.
public class UserPresenceService : IUserPresenceService
{
    public Task SetOnlineAsync(string userId, string username) => throw new NotImplementedException();
    public Task SetOfflineAsync(string userId) => throw new NotImplementedException();
    public Task SetAwayAsync(string userId) => throw new NotImplementedException();
    public Task<UserStatus> GetStatusAsync(string userId) => throw new NotImplementedException();
    public Task<IEnumerable<UserStatus>> GetOnlineUsersAsync() => throw new NotImplementedException();
    public Task<bool> IsOnlineAsync(string userId) => throw new NotImplementedException();
}
