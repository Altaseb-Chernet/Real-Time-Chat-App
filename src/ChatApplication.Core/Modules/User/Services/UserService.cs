using ChatApplication.Core.Common.Exceptions;
using ChatApplication.Core.Dependencies.Constants;
using ChatApplication.Core.Modules.Authentication.Contracts;
using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Models;

namespace ChatApplication.Core.Modules.User.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository) => _userRepository = userRepository;

    public Task<AppUser?> GetByIdAsync(string userId)
        => _userRepository.GetByIdAsync(userId);

    public Task<AppUser?> GetByEmailAsync(string email)
        => _userRepository.GetByEmailAsync(email);

    public async Task UpdateProfileAsync(string userId, UserProfile profile)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppException(ErrorMessages.UserNotFound, 404);

        if (!string.IsNullOrWhiteSpace(profile.DisplayName))
            user.Username = profile.DisplayName;

        if (profile.AvatarUrl is not null)
            user.AvatarUrl = profile.AvatarUrl;

        if (profile.Bio is not null)
            user.Bio = profile.Bio;

        await _userRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppException(ErrorMessages.UserNotFound, 404);

        user.IsDeleted = true;
        await _userRepository.SaveChangesAsync();
    }
}
