using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class KeyExchangeRequest
{
    [Required]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public Guid ToUserId { get; set; }

    [Required]
    public string EncryptedSessionKey { get; set; } = string.Empty;
}
