using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.User.Services;

public class UserService : IUserService
{
    public Task<User?> GetByIdAsync(string userId) => throw new NotImplementedException();
    public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
    public Task UpdateProfileAsync(string userId, UserProfile profile) => throw new NotImplementedException();
    public Task DeleteAsync(string userId) => throw new NotImplementedException();
}
