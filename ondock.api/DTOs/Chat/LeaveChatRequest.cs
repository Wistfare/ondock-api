using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class LeaveChatRequest
{
    [Required]
    public string ChatId { get; set; } = string.Empty;
}
