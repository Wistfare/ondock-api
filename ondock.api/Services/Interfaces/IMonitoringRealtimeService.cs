namespace ondock.api.Services.Interfaces;

public interface IMonitoringRealtimeService
{
    Task SendStatusChangeAsync(
        Guid userId,
        Guid sessionId,
        string deviceId,
        int previousStatus,
        int newStatus);

    Task SendStartStreamingAsync(
        Guid userId,
        string publisherDeviceId,
        Guid sessionId,
        string requesterDeviceId);

    Task SendSessionEndedAsync(
        Guid userId,
        Guid sessionId);
}
