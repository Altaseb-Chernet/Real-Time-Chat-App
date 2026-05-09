namespace ChatApplication.Client.Models;

public record AuthResponse(string Token, string UserId, string Username, DateTime ExpiresAt);

public record MessageDto(
    string Id,
    string Content,
    string SenderId,
    string SenderUsername,
    string RoomId,
    DateTime SentAt,
    bool IsEdited,
    bool IsDeleted,
    string? MediaUrl,
    string? MediaType,
    string? MediaName,
    long? MediaBytes
);

public record ChatRoomDto(string Id, string Name, string CreatedByUserId, DateTime CreatedAt);

public record UserStatusDto(string UserId, string Username, string Status, DateTime LastSeen);

public record RoomMemberDto(string UserId, string Username, DateTime JoinedAt, bool IsCreator);

public record PrivateMessage(
    string Id,
    string SenderId,
    string SenderUsername,
    string RecipientId,
    string Content,
    DateTime SentAt,
    string? MediaUrl,
    string? MediaType,
    string? MediaName,
    long? MediaBytes
);

public record MediaUploadResult(
    string PublicId,
    string Url,
    string MediaType,
    string FileName,
    long Bytes,
    int? Width,
    int? Height,
    double? Duration
);

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
