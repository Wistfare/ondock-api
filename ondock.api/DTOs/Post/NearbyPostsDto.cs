namespace ondock.api.DTOs.Post;

public class NearbyPostsDto
{
    public double UserLatitude { get; set; }
    public double UserLongitude { get; set; }
    public int TotalCount { get; set; }
    public List<PostDto> Posts { get; set; } = new();
}
