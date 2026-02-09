using ondock.api.Data.Entities;
using ondock.api.DTOs.Monitoring;

namespace ondock.api.Services.Interfaces;

public interface IDockLightMonitoringService
{
    Task<DockLightStatusEvent> UpdateStatusAsync(Guid userId, DockLightStatusUpdateRequest request, CancellationToken ct = default);
    Task<DockLightStatusEvent?> GetLatestStatusAsync(Guid userId, CancellationToken ct = default);
}
