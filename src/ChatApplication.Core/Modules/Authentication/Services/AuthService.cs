using ChatApplication.Core.Common.Exceptions;
using ChatApplication.Core.Common.Helpers;
using ChatApplication.Core.Dependencies.Configuration;
using ChatApplication.Core.Dependencies.Constants;
using ChatApplication.Core.Modules.Authentication.Contracts;
using ChatApplication.Core.Modules.Authentication.Models;
using ChatApplication.Core.Modules.Authentication.Validators;
using Microsoft.Extensions.Options;

namespace ChatApplication.Core.Modules.Authentication.Services;

public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly LoginRequestValidator _loginValidator;
    private readonly RegisterRequestValidator _registerValidator;

    public AuthService(
        ITokenService tokenService,
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtSettings,
        LoginRequestValidator loginValidator,
        RegisterRequestValidator registerValidator)
    {
        _tokenService = tokenService;
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var errors = _loginValidator.Validate(request).ToList();
        if (errors.Count > 0)
            throw new ValidationException(errors);

        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new AppException(ErrorMessages.InvalidCredentials, 401);

        if (!EncryptionHelper.VerifyPassword(request.Password, user.PasswordHash))
            throw new AppException(ErrorMessages.InvalidCredentials, 401);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var errors = _registerValidator.Validate(request).ToList();
        if (errors.Count > 0)
            throw new ValidationException(errors);

        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new AppException("Email is already in use.", 409);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = EncryptionHelper.HashPassword(request.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public Task LogoutAsync(string userId)
    {
        // Token-based auth is stateless; invalidation handled client-side.
        // Extend here to blacklist tokens via Redis if needed.
        return Task.CompletedTask;
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role);
        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
        };
    }
}
