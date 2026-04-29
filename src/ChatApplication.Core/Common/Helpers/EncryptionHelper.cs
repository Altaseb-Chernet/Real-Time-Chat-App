using System.Security.Cryptography;
using System.Text;

namespace ChatApplication.Core.Common.Helpers;

public static class EncryptionHelper
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string password, string hash)
        => HashPassword(password) == hash;
}
