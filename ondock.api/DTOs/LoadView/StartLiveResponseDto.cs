namespace ondock.api.DTOs.LoadView;

public class StartLiveResponseDto
{
    public Guid ResponseId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
}
