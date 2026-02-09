using ondock.api.DTOs.LoadView;

namespace ondock.api.Services.Interfaces;

public interface ILoadViewService
{
    Task<LoadViewRequestDto> CreateRequestAsync(Guid userId, CreateLoadViewRequestDto dto);
    Task<LoadViewRequestDto?> GetRequestAsync(Guid requestId, Guid? userId = null);
    Task<List<LoadViewRequestDto>> GetUserRequestsAsync(Guid userId);
    Task<NearbyLoadViewRequestsDto> GetNearbyRequestsAsync(double latitude, double longitude, int radiusMeters = 10000);
    Task<bool> CancelRequestAsync(Guid requestId, Guid userId);
    Task<LoadViewResponseDto> SubmitResponseAsync(Guid requestId, Guid userId, SubmitLoadViewResponseDto dto);
    Task<StartLiveResponseDto> StartLiveResponseAsync(Guid requestId, Guid userId);
    Task<bool> EndLiveResponseAsync(Guid responseId, Guid userId, string? recordingUrl);
    Task<List<LoadViewResponseDto>> GetResponsesAsync(Guid requestId);
    Task<bool> MarkResponseHelpfulAsync(Guid responseId, Guid userId, bool wasHelpful);
    Task<bool> DeleteResponseAsync(Guid responseId, Guid userId);
    Task ExpireOldRequestsAsync(CancellationToken cancellationToken = default);
}
