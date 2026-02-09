namespace ondock.api.DTOs.Auth;

public class ConfirmationCodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Expiration time of the code in minutes (only included when sending code)
    /// </summary>
    public int? ExpiresInMinutes { get; set; }

    /// <summary>
    /// The confirmation method being used ("link" or "code")
    /// </summary>
    public string Method { get; set; } = string.Empty;
}
