using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class RegisterKeyRequest
{
    [Required]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string KeyType { get; set; } = "RSA-2048";
}
