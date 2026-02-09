namespace ondock.api.DTOs.LoadView;

public class NearbyLoadViewRequestsDto
{
    public double UserLatitude { get; set; }
    public double UserLongitude { get; set; }
    public int TotalCount { get; set; }
    public List<LoadViewRequestDto> Requests { get; set; } = new();
}
