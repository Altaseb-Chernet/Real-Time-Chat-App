using ChatApplication.Core.Modules.Authentication.Contracts;

namespace ChatApplication.Core.Modules.Authentication.Services;

public class TokenService : ITokenService
{
    public string GenerateToken(string userId, string email, string role) => throw new NotImplementedException();
    public bool ValidateToken(string token) => throw new NotImplementedException();
    public string? GetUserIdFromToken(string token) => throw new NotImplementedException();
}
