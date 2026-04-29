using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.User.Services;

public class UserPresenceService : IUserPresenceService
{
    public Task SetOnlineAsync(string userId) => throw new NotImplementedException();
    public Task SetOfflineAsync(string userId) => throw new NotImplementedException();
    public Task<UserStatus> GetStatusAsync(string userId) => throw new NotImplementedException();
}
