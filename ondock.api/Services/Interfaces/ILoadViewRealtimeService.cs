using ondock.api.DTOs.LoadView;

namespace ondock.api.Services.Interfaces;

public interface ILoadViewRealtimeService
{
    Task NotifyNewRequestAsync(LoadViewRequestDto request);
    Task NotifyNewResponseAsync(Guid requestId, LoadViewResponseDto response);
    Task NotifyRequestExpiredAsync(Guid requestId);
    Task NotifyLiveStreamStartedAsync(Guid requestId, LoadViewResponseDto response);
    Task NotifyLiveStreamEndedAsync(Guid requestId, Guid responseId);
    Task NotifyRequestFulfilledAsync(Guid requestId);
    Task NotifyNearbyUsersAsync(double latitude, double longitude, LoadViewRequestDto request);
}
