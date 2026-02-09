using ondock.api.Data.Entities;
using ondock.api.DTOs.LoadView;
using ondock.api.DTOs.Notifications;

namespace ondock.api.Services.Interfaces;

public interface IFcmNotificationService
{
    Task RegisterDeviceTokenAsync(Guid userId, RegisterDeviceTokenRequest request, CancellationToken ct = default);

    Task SendDockLightStatusAsync(Guid userId, DockLightStatusType status, string deviceId, DockLightStatusSource source, string? reason, CancellationToken ct = default);
    
    Task SendLoadViewRequestNearbyAsync(Guid userId, LoadViewRequestDto request, double distanceMeters, CancellationToken ct = default);
    Task SendLoadViewResponseReceivedAsync(Guid userId, LoadViewResponseDto response, CancellationToken ct = default);
    Task SendLoadViewLiveStreamStartedAsync(Guid userId, LoadViewResponseDto response, CancellationToken ct = default);

    Task SendChatMessageAsync(IEnumerable<Guid> recipientUserIds, string chatId, Guid messageId, string title, string body, MessageType messageType, CancellationToken ct = default);
}
