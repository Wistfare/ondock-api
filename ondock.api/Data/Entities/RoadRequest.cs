using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ondock.api.Data.Entities;

public enum RequestStatus
{
    Active,
    Fulfilled,
    Expired
}

public class RoadRequest
{
    [Key]
    public Guid RequestId { get; set; }
    
    [Required]
    public Guid RequesterId { get; set; }
    
    [Required]
    [Column(TypeName = "geography(Point)")]
    public Point Location { get; set; } = null!;
    
    public int Radius { get; set; }
    
    [MaxLength(100)]
    public string? RequestType { get; set; }
    
    public UrgencyLevel UrgencyLevel { get; set; }
    
    public RequestStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public int ResponseCount { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(RequesterId))]
    public virtual User Requester { get; set; } = null!;
    
    public virtual ICollection<RoadRequestResponse> Responses { get; set; } = new List<RoadRequestResponse>();
}
