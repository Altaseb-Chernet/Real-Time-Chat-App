namespace ChatApplication.Core.Modules.Chat.Models;

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;

    // Optional media attachment (images/videos/docs).
    public string? MediaUrl { get; set; }
    public string? MediaPublicId { get; set; }
    public string? MediaType { get; set; } // e.g. "image" / "video" / "raw"
    public string? MediaName { get; set; } // original filename
    public long? MediaBytes { get; set; }
}
