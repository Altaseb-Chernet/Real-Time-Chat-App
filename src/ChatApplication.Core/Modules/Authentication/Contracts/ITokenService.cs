namespace ChatApplication.Core.Modules.Authentication.Contracts;

public interface ITokenService
{
    string GenerateToken(string userId, string email, string role);
    bool ValidateToken(string token);
    string? GetUserIdFromToken(string token);
}
