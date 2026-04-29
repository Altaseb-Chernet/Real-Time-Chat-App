using ChatApplication.Core.Common.Exceptions;
using ChatApplication.Core.Dependencies.Constants;
using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;

namespace ChatApplication.Core.Modules.Chat.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IChatRoomRepository _chatRoomRepository;

    public MessageService(IMessageRepository messageRepository, IChatRoomRepository chatRoomRepository)
    {
        _messageRepository = messageRepository;
        _chatRoomRepository = chatRoomRepository;
    }

    public async Task<MessageResponse> SendMessageAsync(SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new AppException("Message content cannot be empty.");

        var room = await _chatRoomRepository.GetByIdAsync(request.RoomId)
            ?? throw new AppException(ErrorMessages.RoomNotFound, 404);

        var isMember = await _chatRoomRepository.IsMemberAsync(request.RoomId, request.SenderId);
        if (!isMember)
            throw new AppException("You are not a member of this room.", 403);

        var message = new Message
        {
            Content = request.Content,
            SenderId = request.SenderId,
            RoomId = request.RoomId,
            SentAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message);
        await _messageRepository.SaveChangesAsync();

        // Reload with sender navigation populated
        var saved = await _messageRepository.GetByIdWithSenderAsync(message.Id);

        return MapToResponse(saved ?? message);
    }

    public async Task<IEnumerable<MessageResponse>> GetMessagesAsync(string roomId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var messages = await _messageRepository.GetByRoomAsync(roomId, skip, pageSize);
        // Return in chronological order (repository returns newest-first for paging)
        return messages.OrderBy(m => m.SentAt).Select(MapToResponse);
    }

    public async Task DeleteMessageAsync(string messageId, string userId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId)
            ?? throw new AppException(ErrorMessages.MessageNotFound, 404);

        if (message.SenderId != userId)
            throw new AppException(ErrorMessages.Unauthorized, 403);

        await _messageRepository.SoftDeleteAsync(messageId);
        await _messageRepository.SaveChangesAsync();
    }

    private static MessageResponse MapToResponse(Message m) => new()
    {
        Id = m.Id,
        Content = m.Content,
        SenderId = m.SenderId,
        SenderUsername = m.Sender?.Username ?? string.Empty,
        RoomId = m.RoomId,
        SentAt = m.SentAt
    };
}
