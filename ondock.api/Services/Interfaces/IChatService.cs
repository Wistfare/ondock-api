using ondock.api.DTOs.Chat;

namespace ondock.api.Services.Interfaces;

public interface IChatService
{
    Task<NearbyUsersResponse> GetNearbyUsersAsync(Guid userId, double latitude, double longitude, double speedMph, double? radiusMiles);
    Task<UserProfileResponse> GetProfileAsync(Guid userId);
    Task<UserProfileResponse> UpsertProfileAsync(Guid userId, UpsertUserProfileRequest request);
    Task<UserProfileResponse> UpsertProfileAsync(Guid userId, UpsertChatProfileRequest request);
    Task<CreateAnnouncementResponse> CreateAnnouncementAsync(Guid userId, CreateAnnouncementRequest request);
    Task<ChatInboxResponse> GetInboxAsync(Guid userId, double? latitude, double? longitude, int take);
    Task<ReplyToAnnouncementResponse> ReplyToAnnouncementAsync(Guid userId, ReplyToAnnouncementRequest request);
    Task<ReportChatResponse> ReportChatAsync(Guid userId, ReportChatRequest request);
    Task<JoinChatResponse> JoinAsync(Guid userId, JoinChatRequest request);
    Task LeaveAsync(Guid userId, LeaveChatRequest request);
    Task<ChatMessageResponse> SendMessageAsync(Guid userId, SendMessageRequest request);
    Task<ChatMessageResponse> SendMediaMessageAsync(Guid userId, SendMessageRequest request, string mediaUrl, string fileName, long fileSize, int? durationSeconds);
    Task<ChatHistoryResponse> GetHistoryAsync(Guid userId, string chatId, int take);
    Task<ActiveChatsResponse> GetActiveChatsAsync(Guid userId, double? latitude = null, double? longitude = null);
    Task UpdatePresenceAsync(Guid userId, string chatId, double? latitude, double? longitude, double? speedMph);
    Task MarkMessagesAsReadAsync(Guid userId, string chatId);
    Task<IReadOnlyList<Guid>> GetChatParticipantUserIdsAsync(string chatId);
}
