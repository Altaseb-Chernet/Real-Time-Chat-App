namespace ChatApplication.Shared.Responses;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string>? Errors { get; set; }
    public int StatusCode { get; set; }
}
