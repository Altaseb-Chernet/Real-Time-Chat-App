namespace ChatApplication.Core.Modules.Chat.Models;

public class MessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }

    // Optional media attachment for the UI.
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? MediaName { get; set; }
    public long? MediaBytes { get; set; }
}
