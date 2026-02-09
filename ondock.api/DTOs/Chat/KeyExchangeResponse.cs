namespace ondock.api.DTOs.Chat;

public class KeyExchangeResponse
{
    public string ChatId { get; set; } = string.Empty;
    public Guid InitiatorUserId { get; set; }
    public Guid TargetUserId { get; set; }
    public string EncryptedKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
