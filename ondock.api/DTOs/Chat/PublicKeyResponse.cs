namespace ondock.api.DTOs.Chat;

public class PublicKeyResponse
{
    public Guid KeyId { get; set; }
    public Guid UserId { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
