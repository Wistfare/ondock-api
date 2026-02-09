namespace ondock.api.Configuration;

public class LiveKitSettings
{
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string DefaultRoomPrefix { get; set; } = "dock";
}
