using System.Security.Claims;
using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatRoomService _chatRoomService;
    private readonly IMessageService _messageService;

    public ChatController(IChatRoomService chatRoomService, IMessageService messageService)
    {
        _chatRoomService = chatRoomService;
        _messageService = messageService;
    }

    // -------------------------------------------------------------------------
    // Rooms
    // -------------------------------------------------------------------------

    [HttpGet("rooms")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ChatRoom>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _chatRoomService.GetRoomsAsync();
        return Ok(ApiResponse<IEnumerable<ChatRoom>>.Ok(rooms));
    }

    [HttpGet("rooms/{roomId}")]
    [ProducesResponseType(typeof(ApiResponse<ChatRoom>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoom(string roomId)
    {
        var room = await _chatRoomService.GetRoomAsync(roomId);
        if (room is null) return NotFound(ApiResponse<ChatRoom>.Fail("Room not found."));
        return Ok(ApiResponse<ChatRoom>.Ok(room));
    }

    [HttpPost("rooms")]
    [ProducesResponseType(typeof(ApiResponse<ChatRoom>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userId = GetUserId();
        var room = await _chatRoomService.CreateRoomAsync(request.Name, userId);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ChatRoom>.Ok(room));
    }

    [HttpDelete("rooms/{roomId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRoom(string roomId)
    {
        await _chatRoomService.DeleteRoomAsync(roomId);
        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Messages
    // -------------------------------------------------------------------------

    [HttpGet("rooms/{roomId}/messages")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MessageResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(string roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var messages = await _messageService.GetMessagesAsync(roomId, page, pageSize);
        return Ok(ApiResponse<IEnumerable<MessageResponse>>.Ok(messages));
    }

    [HttpDelete("messages/{messageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessage(string messageId)
    {
        var userId = GetUserId();
        await _messageService.DeleteMessageAsync(messageId, userId);
        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();
}

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
}
