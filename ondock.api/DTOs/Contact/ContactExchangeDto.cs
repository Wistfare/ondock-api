namespace ondock.api.DTOs.Contact;

public class GenerateQRCodeResponse
{
    public string QRCode { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ScanQRCodeRequest
{
    public string Token { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ScanQRCodeResponse
{
    public bool Success { get; set; }
    public string? ContactId { get; set; }
    public string? DisplayName { get; set; }
    public string? Message { get; set; }
}

public class PermanentContactDto
{
    public string ContactId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime MatchedAt { get; set; }
    public bool IdentityRevealed { get; set; }
}

public class PermanentContactsResponse
{
    public int Count { get; set; }
    public List<PermanentContactDto> Contacts { get; set; } = new();
}
