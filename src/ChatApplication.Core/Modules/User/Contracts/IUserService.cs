namespace ChatApplication.Core.Modules.User.Contracts;

public interface IUserService
{
    Task<AppUser?> GetByIdAsync(string userId);
    Task<AppUser?> GetByEmailAsync(string email);
    Task UpdateProfileAsync(string userId, UserProfile profile);
    Task DeleteAsync(string userId);
}
