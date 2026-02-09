namespace ondock.api.Configuration;

public class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ResetPasswordUrl { get; set; } = string.Empty;

    /// <summary>
    /// Email confirmation method: "link" (default) or "code"
    /// - "link": Sends a confirmation link that user clicks
    /// - "code": Sends a 6-digit confirmation code that user enters
    /// </summary>
    public string ConfirmationMethod { get; set; } = "link";

    /// <summary>
    /// Expiration time in minutes for confirmation codes (default: 15 minutes)
    /// </summary>
    public int ConfirmationCodeExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Length of the confirmation code (default: 6 digits)
    /// </summary>
    public int ConfirmationCodeLength { get; set; } = 6;

    /// <summary>
    /// Check if confirmation method is code-based
    /// </summary>
    public bool UseConfirmationCode => ConfirmationMethod?.ToLowerInvariant() == "code";
}
