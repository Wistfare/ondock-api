using ondock.api.Data.Entities;

namespace ondock.api.Services.Interfaces;

public interface IDockLightStatusProcessor
{
    Task<DockLightStatusEvent> RecordDeviceStatusAsync(Guid userId, string deviceId, DockLightStatusType status, string? rawData, Guid? sessionId, CancellationToken ct = default);
    Task<DockLightStatusEvent> RecordLiveKitStatusAsync(Guid userId, DockLightStatusType status, string reason, Guid? sessionId, CancellationToken ct = default);
    Task<DockLightStatusEvent> RecordSystemStatusAsync(Guid userId, DockLightStatusType status, string reason, Guid? sessionId, CancellationToken ct = default);
}