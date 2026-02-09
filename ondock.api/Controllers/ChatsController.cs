using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Chat;
using ondock.api.Grpc.Services;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

/// <summary>
/// RESTful API controller for chat operations.
/// This controller follows REST conventions and will eventually replace ChatController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[Obsolete("Use gRPC ChatService instead. This REST controller is deprecated.")]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IChatBroadcastService _broadcastService;

    public ChatsController(IChatService chatService, IFileStorageService fileStorageService, IChatBroadcastService broadcastService)
    {
        _chatService = chatService;
        _fileStorageService = fileStorageService;
        _broadcastService = broadcastService;
    }

    #region Chat Operations

    /// <summary>
    /// Get all active chats for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ActiveChatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ActiveChatsResponse>> GetChats(
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null)
    {
        var userId = GetUserId();
        var result = await _chatService.GetActiveChatsAsync(userId, latitude, longitude);
        return Ok(result);
    }

    /// <summary>
    /// Join or create a chat
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(JoinChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JoinChatResponse>> JoinChat([FromBody] JoinChatRequest request)
    {
        var userId = GetUserId();
        var result = await _chatService.JoinAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// Get chat history
    /// </summary>
    [HttpGet("{chatId}/messages")]
    [ProducesResponseType(typeof(ChatHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatHistoryResponse>> GetMessages(string chatId, [FromQuery] int take = 50)
    {
        var userId = GetUserId();
        var result = await _chatService.GetHistoryAsync(userId, chatId, take);
        return Ok(result);
    }

    /// <summary>
    /// Send a message to a chat
    /// </summary>
    [HttpPost("{chatId}/messages")]
    [ProducesResponseType(typeof(ChatMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage(string chatId, [FromBody] SendMessageRequest request)
    {
        var userId = GetUserId();

        // Ensure the chatId in the route matches the request
        if (request.ChatId != chatId)
        {
            request.ChatId = chatId;
        }

        var result = await _chatService.SendMessageAsync(userId, request);
        return CreatedAtAction(nameof(GetMessages), new { chatId }, result);
    }

    /// <summary>
    /// Leave a chat
    /// </summary>
    [HttpDelete("{chatId}/participants/me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveChat(string chatId)
    {
        var userId = GetUserId();
        await _chatService.LeaveAsync(userId, new LeaveChatRequest { ChatId = chatId });
        return NoContent();
    }

    /// <summary>
    /// Update presence/heartbeat for a chat participant
    /// </summary>
    [HttpPost("{chatId}/presence")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePresence(
        string chatId,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double? speedMph = null)
    {
        var userId = GetUserId();
        await _chatService.UpdatePresenceAsync(userId, chatId, latitude, longitude, speedMph);
        return NoContent();
    }

    /// <summary>
    /// Mark all unread messages in a chat as read
    /// </summary>
    [HttpPost("{chatId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(string chatId)
    {
        var userId = GetUserId();
        await _chatService.MarkMessagesAsReadAsync(userId, chatId);
        return NoContent();
    }

    /// <summary>
    /// Report a chat or participant
    /// </summary>
    [HttpPost("{chatId}/reports")]
    [ProducesResponseType(typeof(ReportChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportChatResponse>> ReportChat(string chatId, [FromBody] ReportChatRequest request)
    {
        var userId = GetUserId();

        if (request.ChatId != chatId)
        {
            request.ChatId = chatId;
        }

        var result = await _chatService.ReportChatAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// Upload media (image, audio, video) to a chat
    /// </summary>
    [HttpPost("{chatId}/media")]
    [ProducesResponseType(typeof(UploadMediaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<ActionResult<UploadMediaResponse>> UploadMedia(string chatId, [FromForm] UploadMediaRequest request)
    {
        var userId = GetUserId();

        if (request.ChatId != chatId)
        {
            request.ChatId = chatId;
        }

        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        // Determine message type from content type
        var contentType = request.File.ContentType.ToLowerInvariant();
        MessageType messageType;
        string folder;

        if (contentType.StartsWith("image/"))
        {
            messageType = MessageType.Image;
            folder = "images";
        }
        else if (contentType.StartsWith("audio/"))
        {
            messageType = MessageType.Audio;
            folder = "audio";
        }
        else if (contentType.StartsWith("video/"))
        {
            messageType = MessageType.Video;
            folder = "videos";
        }
        else
        {
            messageType = MessageType.File;
            folder = "files";
        }

        // Upload file
        await using var stream = request.File.OpenReadStream();
        var mediaUrl = await _fileStorageService.UploadAsync(
            stream,
            request.File.FileName,
            request.File.ContentType,
            $"chat/{chatId}/{folder}"
        );

        // Send message with media
        var sendRequest = new SendMessageRequest
        {
            ChatId = chatId,
            EncryptedContent = request.Caption ?? request.File.FileName,
            MessageType = messageType,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SpeedMph = request.SpeedMph
        };

        var messageResult = await _chatService.SendMediaMessageAsync(
            userId,
            sendRequest,
            mediaUrl,
            request.File.FileName,
            request.File.Length,
            request.DurationSeconds
        );

        // Broadcast to gRPC stream subscribers for real-time updates
        await _broadcastService.BroadcastMessageAsync(chatId, messageResult, userId);
        await _broadcastService.BroadcastThreadUpdateAsync(chatId);

        return CreatedAtAction(nameof(GetMessages), new { chatId }, new UploadMediaResponse
        {
            MessageId = messageResult.MessageId.ToString(),
            MediaUrl = mediaUrl,
            MediaType = messageType.ToString().ToLowerInvariant(),
            FileName = request.File.FileName,
            FileSize = request.File.Length,
            DurationSeconds = request.DurationSeconds,
            Timestamp = messageResult.Timestamp
        });
    }

    #endregion

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new ondock.api.Exceptions.UnauthorizedException("Invalid authentication token");
        }
        return userId;
    }
}
