using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ondock.api.Data.Entities;

public class User : IdentityUser<Guid>
{
    // Identity provides: Id, UserName, Email, EmailConfirmed, PasswordHash, 
    // PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, SecurityStamp, etc.
    
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(50)]
    public string? OAuthProvider { get; set; }
    
    [MaxLength(255)]
    public string? OAuthId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual UserProfile? UserProfile { get; set; }
    public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<RoadRequest> RoadRequests { get; set; } = new List<RoadRequest>();
    public virtual ICollection<RoadRequestResponse> RoadRequestResponses { get; set; } = new List<RoadRequestResponse>();
    public virtual ICollection<SavedWaypoint> SavedWaypoints { get; set; } = new List<SavedWaypoint>();
    public virtual ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    public virtual ICollection<UserReward> UserRewards { get; set; } = new List<UserReward>();
    public virtual ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
    public virtual ICollection<DockLightStatusEvent> DockLightStatusEvents { get; set; } = new List<DockLightStatusEvent>();
    public virtual ICollection<MonitoringSession> MonitoringSessions { get; set; } = new List<MonitoringSession>();
    public virtual ICollection<MonitoringSessionParticipant> MonitoringSessionParticipants { get; set; } = new List<MonitoringSessionParticipant>();
    public virtual UserDailyShares? UserDailyShares { get; set; }
    public virtual UserLocation? UserLocation { get; set; }
    public virtual LocationPreferences? LocationPreferences { get; set; }
    public virtual ActiveTruck? ActiveTruck { get; set; }
    public virtual UserSettings? UserSettings { get; set; }
}
