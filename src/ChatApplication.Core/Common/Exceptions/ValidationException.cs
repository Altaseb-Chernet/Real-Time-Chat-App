namespace ChatApplication.Core.Common.Exceptions;

public class ValidationException : AppException
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors) : base("Validation failed", 422)
    {
        Errors = errors;
    }
}
