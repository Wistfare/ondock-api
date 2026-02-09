using ondock.api.DTOs.Notifications;

namespace ondock.api.Services.Interfaces;

public interface IFirebaseMessagingService
{
    Task RegisterDeviceTokenAsync(Guid userId, RegisterDeviceTokenRequest request);
    Task SendStartStreamingNotificationAsync(Guid userId, string deviceId, Guid sessionId, string requesterDeviceId);
    Task SendStatusChangeNotificationAsync(Guid userId, Guid sessionId, string deviceId, int previousStatus, int newStatus);
}
