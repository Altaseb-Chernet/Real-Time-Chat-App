using System.Text.RegularExpressions;
using ChatApplication.Core.Modules.Authentication.Models;

namespace ChatApplication.Core.Modules.Authentication.Validators;

public class RegisterRequestValidator
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IEnumerable<string> Validate(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            yield return "Username is required.";
        else if (request.Username.Length < 3 || request.Username.Length > 50)
            yield return "Username must be between 3 and 50 characters.";

        if (string.IsNullOrWhiteSpace(request.Email))
            yield return "Email is required.";
        else if (!EmailRegex.IsMatch(request.Email))
            yield return "Email is not valid.";

        if (string.IsNullOrWhiteSpace(request.Password))
            yield return "Password is required.";
        else if (request.Password.Length < 8)
            yield return "Password must be at least 8 characters.";
    }
}
