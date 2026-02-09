using ondock.api.DTOs.Post;

namespace ondock.api.Services.Interfaces;

public interface IPostService
{
    Task<PostDto> CreatePostAsync(Guid userId, CreatePostDto dto);
    Task<PostDto?> GetPostAsync(Guid postId);
    Task<List<PostDto>> GetUserPostsAsync(Guid userId);
    Task<List<PostDto>> GetPostsByUserIdsAsync(List<Guid> userIds);
    Task<bool> DeletePostAsync(Guid postId, Guid userId);
    Task<bool> IncrementViewCountAsync(Guid postId);
    Task<bool> ReportPostAsync(Guid postId, Guid userId);
    Task ExpireOldPostsAsync(CancellationToken cancellationToken = default);
}
