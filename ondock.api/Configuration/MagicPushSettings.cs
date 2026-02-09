namespace ondock.api.Configuration;

public class MagicPushSettings
{
    public string? BaseUrl { get; set; }
    public string? AppHash { get; set; }
    public string? ApiKey { get; set; }
    public string? MonitoringCompletedEventName { get; set; } = "monitoring_completed";
    public string? ChatMessageTitle { get; set; } = "New message";
    public string? ChatMessageEventName { get; set; } = "chat_message";
}
