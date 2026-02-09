using ondock.api.DTOs.Chat;

namespace ondock.api.Services.Interfaces;

public interface IAnnouncementService
{
    Task<IReadOnlyList<AnnouncementDto>> GetAnnouncementsAsync(Guid userId, double latitude, double longitude, int take = 50);
    Task<AnnouncementDto?> GetAnnouncementAsync(Guid announcementId);
    Task<CreateAnnouncementResponse> CreateAnnouncementAsync(Guid userId, CreateAnnouncementRequest request);
    Task<bool> DeleteAnnouncementAsync(Guid userId, Guid announcementId);
    Task IncrementViewCountAsync(Guid announcementId, Guid viewerId);
    Task IncrementReplyCountAsync(Guid announcementId);
}
