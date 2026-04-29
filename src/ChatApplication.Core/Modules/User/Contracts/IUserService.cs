namespace ChatApplication.Core.Modules.User.Contracts;

public interface IUserService
{
    Task<User?> GetByIdAsync(string userId);
    Task<User?> GetByEmailAsync(string email);
    Task UpdateProfileAsync(string userId, UserProfile profile);
    Task DeleteAsync(string userId);
}
