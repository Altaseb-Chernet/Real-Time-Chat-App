using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Validators;

public class SendMessageValidator
{
    public IEnumerable<string> Validate(SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content)) yield return "Content is required.";
        if (string.IsNullOrWhiteSpace(request.SenderId)) yield return "SenderId is required.";
        if (string.IsNullOrWhiteSpace(request.RoomId)) yield return "RoomId is required.";
    }
}
