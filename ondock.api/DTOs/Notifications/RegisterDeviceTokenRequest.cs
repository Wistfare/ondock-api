namespace ondock.api.DTOs.Notifications;

public class RegisterDeviceTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android";
    public bool IsPrimary { get; set; } = true;
}
