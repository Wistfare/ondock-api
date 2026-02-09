namespace ondock.api.DTOs.Chat;

public class QRCodeResponse
{
    public string QRCodeData { get; set; } = string.Empty;
    public string MatchCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
