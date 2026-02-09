using ondock.api.Data.Entities;

namespace ondock.api.Services.Interfaces;

public interface ILiveKitService
{
    // Placeholder: In future we'll publish events to LiveKit rooms for real-time sync beyond push notifications.
    Task PublishDockLightStatusAsync(Guid userId, DockLightStatusType status, string deviceId, DockLightStatusSource source, string? reason, CancellationToken ct = default);
    Task<LiveKitAccessTokenResponse> GenerateAccessTokenAsync(string roomName, string identity, bool isPublisher, CancellationToken ct = default);
}

public class LiveKitAccessTokenResponse
{
    public string Room { get; set; } = null!;
    public string Identity { get; set; } = null!;
    public string Token { get; set; } = null!; // JWT for LiveKit
    public DateTime ExpiresAt { get; set; }
}
