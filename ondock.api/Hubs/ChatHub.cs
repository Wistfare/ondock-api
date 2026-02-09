using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ondock.api.Data;

namespace ondock.api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly OnDockDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    // Track which chats each connection is in: ConnectionId -> Set of ChatIds
    private static readonly ConcurrentDictionary<string, HashSet<string>> ConnectionChats = new();

    public ChatHub(OnDockDbContext db, ILogger<ChatHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        ConnectionChats[Context.ConnectionId] = new HashSet<string>();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Update LastHeartbeat to a past time for all chats this connection was in
        if (ConnectionChats.TryRemove(Context.ConnectionId, out var chatIds))
        {
            var userId = GetUserIdSafe();
            if (userId.HasValue && chatIds.Count > 0)
            {
                foreach (var chatId in chatIds)
                {
                    await UpdateParticipantHeartbeatAsync(userId.Value, chatId);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(string chatId)
    {
        var userId = GetUserId();
        var participant = await _db.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
        
        if (participant == null)
        {
            throw new Exception("Not a participant of this chat");
        }

        // Update heartbeat when joining
        participant.LastHeartbeat = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Track this connection's chat membership
        if (ConnectionChats.TryGetValue(Context.ConnectionId, out var chats))
        {
            chats.Add(chatId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{chatId}");
        _logger.LogDebug("User {UserId} joined chat {ChatId}, heartbeat updated", userId, chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        var userId = GetUserIdSafe();
        
        // Remove from tracking
        if (ConnectionChats.TryGetValue(Context.ConnectionId, out var chats))
        {
            chats.Remove(chatId);
        }

        // Update heartbeat on leave
        if (userId.HasValue)
        {
            await UpdateParticipantHeartbeatAsync(userId.Value, chatId);
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{chatId}");
    }

    public async Task SendTypingStarted(string chatId)
    {
        var userId = GetUserId();
        var participant = await _db.ChatParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
        
        if (participant == null) return;

        await Clients.OthersInGroup($"chat:{chatId}").SendAsync("TypingStarted", new
        {
            chatId,
            participantId = participant.ParticipantId.ToString(),
            displayName = $"User {userId.ToString()[..4]}"
        });
    }

    public async Task SendTypingStopped(string chatId)
    {
        var userId = GetUserId();
        var participant = await _db.ChatParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
        
        if (participant == null) return;

        await Clients.OthersInGroup($"chat:{chatId}").SendAsync("TypingStopped", new
        {
            chatId,
            participantId = participant.ParticipantId.ToString()
        });
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new Exception("Invalid authentication token");
        }

        return userId;
    }

    private Guid? GetUserIdSafe()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    private async Task UpdateParticipantHeartbeatAsync(Guid userId, string chatId)
    {
        try
        {
            var participant = await _db.ChatParticipants
                .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
            
            if (participant != null)
            {
                participant.LastHeartbeat = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogDebug("Updated heartbeat for user {UserId} in chat {ChatId}", userId, chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update heartbeat for user {UserId} in chat {ChatId}", userId, chatId);
        }
    }
}
