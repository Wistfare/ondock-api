using ondock.api.Data.Entities;

namespace ondock.api.DTOs.LoadView;

public class LoadViewRequestDto
{
    public Guid RequestId { get; set; }
    public Guid RequesterId { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterProfilePicture { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public string? RequestType { get; set; }
    public string? Description { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ResponseCount { get; set; }
    public double? DistanceMeters { get; set; }
    public List<LoadViewResponseDto> Responses { get; set; } = new();
}
