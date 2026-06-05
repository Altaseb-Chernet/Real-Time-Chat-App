using System.Security.Claims;
using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Shared.Responses;
using ChatApplication.API.Hubs;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatRoomService _chatRoomService;
    private readonly IMessageService _messageService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ApplicationDbContext _db;

    public ChatController(
        IChatRoomService chatRoomService,
        IMessageService messageService,
        IHubContext<ChatHub> hubContext,
        ApplicationDbContext db)
    {
        _chatRoomService = chatRoomService;
        _messageService = messageService;
        _hubContext      = hubContext;
        _db = db;
    }

    // -------------------------------------------------------------------------
    // Rooms
    // -------------------------------------------------------------------------

    /// <summary>List all chat rooms.</summary>
    [HttpGet("rooms")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ChatRoomDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _chatRoomService.GetRoomsAsync();
        return Ok(ApiResponse<IEnumerable<ChatRoomDto>>.Ok(rooms.Select(MapRoom)));
    }

    /// <summary>Get a single room by ID.</summary>
    [HttpGet("rooms/{roomId}")]
    [ProducesResponseType(typeof(ApiResponse<ChatRoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoom(string roomId)
    {
        var room = await _chatRoomService.GetRoomAsync(roomId);
        if (room is null)
            return NotFound(ApiResponse<ChatRoomDto>.Fail("Room not found."));
        return Ok(ApiResponse<ChatRoomDto>.Ok(MapRoom(room)));
    }

    /// <summary>Create a new chat room. The caller is automatically added as a member.</summary>
    [HttpPost("rooms")]
    [ProducesResponseType(typeof(ApiResponse<ChatRoomDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userId = GetUserId();
        var room = await _chatRoomService.CreateRoomAsync(request.Name, userId);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ChatRoomDto>.Ok(MapRoom(room)));
    }

    /// <summary>Join an existing room.</summary>
    [HttpPost("rooms/{roomId}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinRoom(string roomId)
    {
        var userId = GetUserId();
        await _chatRoomService.JoinRoomAsync(roomId, userId);
        return NoContent();
    }

    /// <summary>Leave a room.</summary>
    [HttpPost("rooms/{roomId}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveRoom(string roomId)
    {
        var userId = GetUserId();
        await _chatRoomService.LeaveRoomAsync(roomId, userId);
        return NoContent();
    }

    /// <summary>Delete a room (creator only — enforced at service layer).</summary>
    [HttpDelete("rooms/{roomId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoom(string roomId)
    {
        await _chatRoomService.DeleteRoomAsync(roomId);
        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Messages
    // -------------------------------------------------------------------------

    /// <summary>Get paginated message history for a room.</summary>
    [HttpGet("rooms/{roomId}/messages")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MessageResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        string roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var messages = await _messageService.GetMessagesAsync(roomId, page, pageSize);
        var list = messages.ToList();

        var paged = new PagedResponse<MessageResponse>
        {
            Items = list,
            TotalCount = list.Count,   // real total requires a count query — good enough for now
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResponse<MessageResponse>>.Ok(paged));
    }

    /// <summary>Get private-message history with another user.</summary>
    [HttpGet("dms/{otherUserId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PrivateMessageResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrivateMessages(string otherUserId, [FromQuery] int take = 200)
    {
        var userId = GetUserId();
        if (take is < 1 or > 500) take = 200;

        var history = await _db.PrivateMessages
            .AsNoTracking()
            .Where(m => !m.IsDeleted &&
                        ((m.SenderId == userId && m.RecipientId == otherUserId) ||
                         (m.SenderId == otherUserId && m.RecipientId == userId)))
            .OrderBy(m => m.SentAt)
            .Take(take)
            .Select(m => new PrivateMessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderUsername = m.Sender.Username,
                RecipientId = m.RecipientId,
                Content = m.Content,
                SentAt = m.SentAt,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                MediaName = m.MediaName,
                MediaBytes = m.MediaBytes
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PrivateMessageResponse>>.Ok(history));
    }

    /// <summary>Send a message to a room via REST (alternative to SignalR).</summary>
    [HttpPost("rooms/{roomId}/messages")]
    [ProducesResponseType(typeof(ApiResponse<MessageResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendMessage(string roomId, [FromBody] SendMessageBody body)
    {
        var userId = GetUserId();
        var response = await _messageService.SendMessageAsync(new SendMessageRequest
        {
            Content = body.Content,
            SenderId = userId,
            RoomId = roomId,
            MediaUrl      = body.MediaUrl,
            MediaPublicId = body.MediaPublicId,
            MediaType     = body.MediaType,
            MediaName     = body.MediaName,
            MediaBytes    = body.MediaBytes
        });

        // REST send is an alternative to SignalR hub send — broadcast manually.
        await _hubContext.Clients.Group(RoomGroup(roomId))
                           .SendAsync(HubEvents.ReceiveMessage, response);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<MessageResponse>.Ok(response));
    }

    /// <summary>Edit a message (sender only).</summary>
    [HttpPut("messages/{messageId}")]
    [ProducesResponseType(typeof(ApiResponse<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditMessage(string messageId, [FromBody] EditMessageBody body)
    {
        var userId   = GetUserId();
        var response = await _messageService.EditMessageAsync(messageId, userId, body.Content);
        
        // Broadcast the edited message
        await _hubContext.Clients.Group(RoomGroup(response.RoomId))
                           .SendAsync(HubEvents.ReceiveMessage, response);
                           
        return Ok(ApiResponse<MessageResponse>.Ok(response));
    }

    /// <summary>Soft-delete a message (sender only).</summary>
    [HttpDelete("messages/{messageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(string messageId)
    {
        var userId = GetUserId();
        var response = await _messageService.DeleteMessageAsync(messageId, userId);
        
        // Broadcast the deleted message (so clients can update UI to show it as deleted)
        await _hubContext.Clients.Group(RoomGroup(response.RoomId))
                           .SendAsync(HubEvents.ReceiveMessage, response);
                           
        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Members
    // -------------------------------------------------------------------------

    /// <summary>List members of a room.</summary>
    [HttpGet("rooms/{roomId}/members")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoomMemberDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomMembers(string roomId)
    {
        var members = await _chatRoomService.GetMembersAsync(roomId);
        return Ok(ApiResponse<IReadOnlyList<RoomMemberDto>>.Ok(members));
    }

    /// <summary>Remove a user from a room (creator only).</summary>
    [HttpDelete("rooms/{roomId}/members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> KickMember(string roomId, string userId)
    {
        var actorId = GetUserId();
        await _chatRoomService.KickMemberAsync(roomId, actorId, userId);
        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    private static ChatRoomDto MapRoom(ChatRoom r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        CreatedByUserId = r.CreatedByUserId,
        CreatedAt = r.CreatedAt
    };

    private static string RoomGroup(string roomId) => $"room:{roomId}";
}

// -------------------------------------------------------------------------
// Request / DTO types local to this controller
// -------------------------------------------------------------------------

public class CreateRoomRequest
{
    /// <example>General</example>
    public string Name { get; set; } = string.Empty;
}

public class SendMessageBody
{
    /// <example>Hello everyone!</example>
    public string Content { get; set; } = string.Empty;

    public string? MediaUrl { get; set; }
    public string? MediaPublicId { get; set; }
    public string? MediaType { get; set; }
    public string? MediaName { get; set; }
    public long? MediaBytes { get; set; }
}

public class EditMessageBody
{
    public string Content { get; set; } = string.Empty;
}

public class PrivateMessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? MediaName { get; set; }
    public long? MediaBytes { get; set; }
}

public class ChatRoomDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
