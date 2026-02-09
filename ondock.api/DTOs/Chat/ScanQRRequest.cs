using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class ScanQRRequest
{
    [Required]
    public string QRCodeData { get; set; } = string.Empty;

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }
}
