using ondock.api.DTOs.Monitoring;

namespace ondock.api.Services.Interfaces;

public interface IMonitoringService
{
    Task<MonitoringSessionResponse> StartSessionAsync(Guid userId, StartMonitoringSessionRequest request);
    Task<MonitoringSessionResponse> EndSessionAsync(Guid userId, Guid sessionId);
    Task<IReadOnlyList<MonitoringSessionResponse>> GetSessionsAsync(Guid userId, int take);
    Task JoinSessionAsync(Guid userId, Guid sessionId, JoinMonitoringSessionRequest request);
    Task UpdateStatusAsync(Guid userId, MonitoringStatusRequest request);
    Task RequestStreamAsync(Guid userId, Guid sessionId, RequestStreamRequest request);
    Task UpdateStreamStateAsync(Guid userId, Guid sessionId, UpdateStreamStateRequest request);
    Task<LiveKitTokenResponse> GetLiveKitTokenAsync(Guid userId, Guid sessionId, string? deviceId);
}
