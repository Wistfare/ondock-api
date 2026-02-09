using Microsoft.AspNetCore.SignalR;
using ondock.api.Hubs;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class SignalRChatRealtimeService : IChatRealtimeService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<SignalRChatRealtimeService> _logger;

    public SignalRChatRealtimeService(IHubContext<ChatHub> hubContext, ILogger<SignalRChatRealtimeService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendMessageReceivedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("MessageReceived", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MessageReceived to chat {ChatId}", chatId);
        }
    }

    public async Task SendChatEndedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("ChatEnded", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ChatEnded to chat {ChatId}", chatId);
        }
    }

    public async Task SendParticipantJoinedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("ParticipantJoined", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ParticipantJoined to chat {ChatId}", chatId);
        }
    }

    public async Task SendParticipantLeftAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("ParticipantLeft", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ParticipantLeft to chat {ChatId}", chatId);
        }
    }

    public async Task SendIdentityRevealRequestedAsync(string chatId, Guid targetUserId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("IdentityRevealRequested", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IdentityRevealRequested to chat {ChatId}", chatId);
        }
    }

    public async Task SendIdentityRevealedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("IdentityRevealed", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IdentityRevealed to chat {ChatId}", chatId);
        }
    }

    public async Task SendAnnouncementReceivedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("AnnouncementReceived", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send AnnouncementReceived to chat {ChatId}", chatId);
        }
    }

    public async Task SendTypingStartedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("TypingStarted", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TypingStarted to chat {ChatId}", chatId);
        }
    }

    public async Task SendTypingStoppedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("TypingStopped", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TypingStopped to chat {ChatId}", chatId);
        }
    }

    public async Task SendParticipantLocationUpdatedAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("ParticipantLocationUpdated", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ParticipantLocationUpdated to chat {ChatId}", chatId);
        }
    }

    public async Task SendNearbyUsersUpdatedAsync(Guid userId, object payload)
    {
        try
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("NearbyUsersUpdated", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send NearbyUsersUpdated to user {UserId}", userId);
        }
    }

    public async Task SendMessagesReadAsync(string chatId, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"chat:{chatId}").SendAsync("MessagesRead", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MessagesRead to chat {ChatId}", chatId);
        }
    }
}
