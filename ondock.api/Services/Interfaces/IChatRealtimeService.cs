namespace ondock.api.Services.Interfaces;

public interface IChatRealtimeService
{
    // Chat messages
    Task SendMessageReceivedAsync(string chatId, object payload);
    Task SendChatEndedAsync(string chatId, object payload);

    // Participant events
    Task SendParticipantJoinedAsync(string chatId, object payload);
    Task SendParticipantLeftAsync(string chatId, object payload);

    // Identity reveal
    Task SendIdentityRevealRequestedAsync(string chatId, Guid targetUserId, object payload);
    Task SendIdentityRevealedAsync(string chatId, object payload);

    // Announcements
    Task SendAnnouncementReceivedAsync(string chatId, object payload);

    // Typing indicators
    Task SendTypingStartedAsync(string chatId, object payload);
    Task SendTypingStoppedAsync(string chatId, object payload);

    // Location updates
    Task SendParticipantLocationUpdatedAsync(string chatId, object payload);

    // Discovery
    Task SendNearbyUsersUpdatedAsync(Guid userId, object payload);

    // Read receipts
    Task SendMessagesReadAsync(string chatId, object payload);
}
