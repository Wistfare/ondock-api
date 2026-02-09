using ondock.api.Data.Entities;
using ondock.api.DTOs.Monitoring;

namespace ondock.api.Services.Interfaces;

public interface IMonitoringSessionService
{
    Task<MonitoringSession> StartSessionAsync(Guid userId, string initiatorDeviceId, string? metadata = null, CancellationToken ct = default);
    Task<MonitoringSession?> EndSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<MonitoringSession?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<MonitoringSession>> ListSessionsAsync(Guid userId, int take = 25, CancellationToken ct = default);
    Task<MonitoringSession?> GetActiveSessionAsync(Guid userId, CancellationToken ct = default);
    Task<bool> RemoveDeviceAsync(Guid userId, string deviceId, CancellationToken ct = default);
    Task<MonitoringSessionParticipant> AddViewerAsync(Guid requestingUserId, Guid sessionId, string viewerDeviceId, CancellationToken ct = default);
}
