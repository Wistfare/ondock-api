using ondock.api.DTOs.Posts;

namespace ondock.api.Services.Interfaces;

public interface IUserStatusService
{
    Task<PostDto> CreateStatusAsync(Guid userId, CreatePostRequest request);
    Task<List<PostDto>> GetMyStatusesAsync(Guid userId);
    Task<UserPostsResponse> GetUserStatusesAsync(Guid viewerUserId, string anonymousUserId);
    Task<List<NearbyUserWithPostsDto>> GetNearbyUsersWithStatusesAsync(Guid userId, double latitude, double longitude, double radiusMiles);
    Task<bool> DeleteStatusAsync(Guid userId, Guid postId);
    Task RecordViewAsync(Guid viewerUserId, Guid postId);
    Task RecordViewsBatchAsync(Guid viewerUserId, List<Guid> postIds);
    Task<int> GetUserStatusCountAsync(Guid userId);
    Task CleanupExpiredStatusesAsync(CancellationToken ct = default);
}
