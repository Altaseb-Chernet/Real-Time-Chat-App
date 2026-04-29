using ChatApplication.Core.Modules.User.Contracts;

namespace ChatApplication.Core.Modules.User.Services;

public class UserService : IUserService
{
    public Task<AppUser?> GetByIdAsync(string userId) => throw new NotImplementedException();
    public Task<AppUser?> GetByEmailAsync(string email) => throw new NotImplementedException();
    public Task UpdateProfileAsync(string userId, UserProfile profile) => throw new NotImplementedException();
    public Task DeleteAsync(string userId) => throw new NotImplementedException();
}
