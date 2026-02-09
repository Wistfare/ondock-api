using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ondock.api.Data.Entities;

namespace ondock.api.Data;

public class OnDockDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public OnDockDbContext(DbContextOptions<OnDockDbContext> options) : base(options)
    {
    }

    // DbSets for all entities (Users inherited from IdentityDbContext)
    // public new DbSet<User> Users { get; set; } - Not needed, inherited from IdentityDbContext
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<LocationPreferences> LocationPreferences { get; set; }
    // Profile Categorization
    public DbSet<ProfileCategory> ProfileCategories { get; set; }
    public DbSet<ProfileSubCategory> ProfileSubCategories { get; set; }
    public DbSet<VehicleType> VehicleTypes { get; set; }
    public DbSet<VehicleBrand> VehicleBrands { get; set; }

    // Chat System
    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Announcement> Announcements { get; set; }

    // Identity & Encryption
    public DbSet<UserMatch> UserMatches { get; set; }
    public DbSet<UserEncryptionKey> UserEncryptionKeys { get; set; }
    public DbSet<ChatKeyExchange> ChatKeyExchanges { get; set; }
    public DbSet<ActiveTruck> ActiveTrucks { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<RoadRequest> RoadRequests { get; set; }
    public DbSet<RoadRequestResponse> RoadRequestResponses { get; set; }
    public DbSet<UserDailyShares> UserDailyShares { get; set; }
    public DbSet<UserLocation> UserLocations { get; set; }
    public DbSet<SavedWaypoint> SavedWaypoints { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<Reward> Rewards { get; set; }
    public DbSet<UserReward> UserRewards { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<EmailConfirmationCode> EmailConfirmationCodes { get; set; }
    public DbSet<MonitoringSession> MonitoringSessions { get; set; }
    public DbSet<MonitoringSessionParticipant> MonitoringSessionParticipants { get; set; }
    public DbSet<MonitoringStatusEvent> MonitoringStatusEvents { get; set; }
    public DbSet<DockLightStatusEvent> DockLightStatusEvents { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    
    // User Status/Stories (WhatsApp-like)
    public DbSet<UserPost> UserPosts { get; set; }
    public DbSet<PostView> PostViews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable PostGIS extension for geography types
        modelBuilder.HasPostgresExtension("postgis");

        // Configure Identity table names to use ASP.NET Core Identity conventions
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastLoginAt).HasColumnType("timestamp");

            // Map UserId to Id for backward compatibility in relationships
            // All FKs still reference UserId but point to Id column
        });

        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserRoles");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserClaims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserLogins");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserTokens");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("AspNetRoleClaims");
        });

        // UserSession entity configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasIndex(e => e.RefreshToken);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id) // Map to Identity's Id property
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProfileCategory entity configuration
        modelBuilder.Entity<ProfileCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ProfileSubCategory entity configuration
        modelBuilder.Entity<ProfileSubCategory>(entity =>
        {
            entity.HasKey(e => e.SubCategoryId);
            entity.HasIndex(e => new { e.CategoryId, e.Name }).IsUnique();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VehicleType entity configuration
        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.HasKey(e => e.VehicleTypeId);
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.SubCategory)
                .WithMany(s => s.VehicleTypes)
                .HasForeignKey(e => e.SubCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // VehicleBrand entity configuration
        modelBuilder.Entity<VehicleBrand>(entity =>
        {
            entity.HasKey(e => e.BrandId);
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.VehicleType)
                .WithMany(v => v.VehicleBrands)
                .HasForeignKey(e => e.VehicleTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserProfile entity configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(e => e.UserId)
                .HasPrincipalKey<User>(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.UserProfiles)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SubCategory)
                .WithMany(s => s.UserProfiles)
                .HasForeignKey(e => e.SubCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.VehicleType)
                .WithMany(v => v.UserProfiles)
                .HasForeignKey(e => e.VehicleTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.VehicleBrand)
                .WithMany(b => b.UserProfiles)
                .HasForeignKey(e => e.VehicleBrandId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // LocationPreferences entity configuration
        modelBuilder.Entity<LocationPreferences>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithOne(u => u.LocationPreferences)
                .HasForeignKey<LocationPreferences>(e => e.UserId)
                .HasPrincipalKey<User>(u => u.Id) // Map to Identity's Id property
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Chat entity configuration
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId);
            entity.HasIndex(e => e.CenterLocation).HasMethod("gist");
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.RadiusMeters).HasDefaultValue(1609);
            entity.Ignore(e => e.IsActive); // Computed property, not in DB
        });

        // ChatParticipant entity configuration
        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasKey(e => e.ParticipantId);
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CurrentLocation).HasMethod("gist");
            entity.Property(e => e.LastHeartbeat).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Chat)
                .WithMany(c => c.Participants)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ChatParticipants)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);

        });

        // Message entity configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.HasIndex(e => new { e.ChatId, e.Timestamp });
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.MessageType).HasDefaultValue(MessageType.Text);

            entity.HasOne(e => e.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany(p => p.Messages)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Announcement entity configuration
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId);
            entity.HasIndex(e => e.OriginLocation).HasMethod("gist");
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.CreatedAt, e.ExpiresAt });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.RadiusMiles).HasDefaultValue(1.0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UrgencyLevel)
                .HasDefaultValue(UrgencyLevel.Medium)
                .HasSentinel(UrgencyLevel.Low);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserMatch entity configuration
        modelBuilder.Entity<UserMatch>(entity =>
        {
            entity.HasKey(e => e.MatchId);
            entity.HasIndex(e => new { e.User1Id, e.User2Id }).IsUnique();
            entity.HasIndex(e => e.MatchCode);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.InitiatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValue(MatchStatus.Pending);

            entity.HasOne(e => e.User1)
                .WithMany()
                .HasForeignKey(e => e.User1Id)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User2)
                .WithMany()
                .HasForeignKey(e => e.User2Id)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Chat)
                .WithMany()
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserEncryptionKey entity configuration
        modelBuilder.Entity<UserEncryptionKey>(entity =>
        {
            entity.HasKey(e => e.KeyId);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatKeyExchange entity configuration
        modelBuilder.Entity<ChatKeyExchange>(entity =>
        {
            entity.HasKey(e => e.ExchangeId);
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => new { e.FromUserId, e.ToUserId });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Chat)
                .WithMany()
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FromUser)
                .WithMany()
                .HasForeignKey(e => e.FromUserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ToUser)
                .WithMany()
                .HasForeignKey(e => e.ToUserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ActiveTruck entity configuration
        modelBuilder.Entity<ActiveTruck>(entity =>
        {
            entity.HasKey(e => e.TruckId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.CurrentLocation).HasMethod("gist");
            entity.Property(e => e.LastMovementAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithOne(u => u.ActiveTruck)
                .HasForeignKey<ActiveTruck>(e => e.UserId)
                .HasPrincipalKey<User>(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Post entity configuration
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId);
            entity.HasIndex(e => e.Location).HasMethod("gist");
            entity.HasIndex(e => e.RoadIdentification);
            entity.HasIndex(e => new { e.CreatedAt, e.ExpiresAt });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoadRequest (LoadView) entity configuration
        modelBuilder.Entity<RoadRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);
            entity.HasIndex(e => e.Location).HasMethod("gist");
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Requester)
                .WithMany(u => u.RoadRequests)
                .HasForeignKey(e => e.RequesterId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoadRequestResponse (LoadView Response) entity configuration
        modelBuilder.Entity<RoadRequestResponse>(entity =>
        {
            entity.HasKey(e => e.ResponseId);
            entity.HasIndex(e => new { e.RequestId, e.CreatedAt });
            entity.HasIndex(e => e.ResponderId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsLiveStream).HasDefaultValue(false);
            entity.Property(e => e.RewardPoints).HasDefaultValue(0);

            entity.HasOne(e => e.Request)
                .WithMany(r => r.Responses)
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Responder)
                .WithMany(u => u.RoadRequestResponses)
                .HasForeignKey(e => e.ResponderId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserDailyShares entity configuration
        modelBuilder.Entity<UserDailyShares>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Date });

            entity.HasOne(e => e.User)
                .WithOne(u => u.UserDailyShares)
                .HasForeignKey<UserDailyShares>(e => e.UserId)
                .HasPrincipalKey<User>(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserLocation entity configuration
        modelBuilder.Entity<UserLocation>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.CurrentLocation).HasMethod("gist");
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithOne(u => u.UserLocation)
                .HasForeignKey<UserLocation>(e => e.UserId)
                .HasPrincipalKey<User>(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SavedWaypoint entity configuration
        modelBuilder.Entity<SavedWaypoint>(entity =>
        {
            entity.HasKey(e => e.WaypointId);
            entity.HasIndex(e => e.Location).HasMethod("gist");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.SavedWaypoints)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Achievement entity configuration
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.AchievementId);
            entity.HasIndex(e => new { e.UserId, e.Type });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Achievements)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Reward entity configuration
        modelBuilder.Entity<Reward>(entity =>
        {
            entity.HasKey(e => e.RewardId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UserReward entity configuration
        modelBuilder.Entity<UserReward>(entity =>
        {
            entity.HasKey(e => e.UserRewardId);
            entity.HasIndex(e => new { e.UserId, e.RewardId }).IsUnique();
            entity.Property(e => e.ClaimedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRewards)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Reward)
                .WithMany(r => r.UserRewards)
                .HasForeignKey(e => e.RewardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NotificationLog entity configuration
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.HasIndex(e => new { e.UserId, e.SentAt });
            entity.Property(e => e.SentAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.NotificationLogs)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DeviceToken entity configuration
        modelBuilder.Entity<DeviceToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.RegisteredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastUsedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.DeviceTokens)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MonitoringSession entity configuration
        modelBuilder.Entity<MonitoringSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EndedAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.MonitoringSessions)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MonitoringSessionParticipant entity configuration
        modelBuilder.Entity<MonitoringSessionParticipant>(entity =>
        {
            entity.HasKey(e => e.ParticipantId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Participants)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.MonitoringSessionParticipants)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MonitoringStatusEvent entity configuration
        modelBuilder.Entity<MonitoringStatusEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.HasIndex(e => new { e.SessionId, e.CreatedAt });
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UserSettings entity configuration
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.SettingsId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DndEnabled).HasDefaultValue(false);
            entity.Property(e => e.DndScheduleEnabled).HasDefaultValue(false);
            entity.Property(e => e.NotifyChat).HasDefaultValue(true);
            entity.Property(e => e.NotifyAnnouncements).HasDefaultValue(true);
            entity.Property(e => e.NotifyNearbyUsers).HasDefaultValue(true);
            entity.Property(e => e.NotifyDockStatus).HasDefaultValue(true);
            entity.Property(e => e.NotifyRoadAlerts).HasDefaultValue(true);
            entity.Property(e => e.NotificationSound).HasDefaultValue(true);
            entity.Property(e => e.NotificationVibration).HasDefaultValue(true);
            entity.Property(e => e.VisibleToNearby).HasDefaultValue(true);
            entity.Property(e => e.ShareLocationInChat).HasDefaultValue(true);
            entity.Property(e => e.ShowOnlineStatus).HasDefaultValue(true);
            entity.Property(e => e.ShowReadReceipts).HasDefaultValue(true);
            entity.Property(e => e.DefaultChatRadiusMiles).HasDefaultValue(0.5);
            entity.Property(e => e.AutoJoinNearbyChats).HasDefaultValue(false);

            entity.HasOne(e => e.User)
                .WithOne(u => u.UserSettings)
                .HasForeignKey<UserSettings>(e => e.UserId)
                .HasPrincipalKey<User>(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPost (Status/Story) entity configuration
        modelBuilder.Entity<UserPost>(entity =>
        {
            entity.HasKey(e => e.PostId);
            entity.HasIndex(e => new { e.UserId, e.IsActive, e.ExpiresAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PostView entity configuration
        modelBuilder.Entity<PostView>(entity =>
        {
            entity.HasKey(e => e.ViewId);
            entity.HasIndex(e => new { e.PostId, e.ViewerId }).IsUnique();
            entity.HasIndex(e => e.ViewerId);
            entity.Property(e => e.ViewedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Post)
                .WithMany(p => p.Views)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Viewer)
                .WithMany()
                .HasForeignKey(e => e.ViewerId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        SeedProfileData(modelBuilder);
    }

    private static void SeedProfileData(ModelBuilder modelBuilder)
    {
        // ============================================
        // PROFILE CATEGORIES (Road Users)
        // ============================================
        var commercialTruckDriverId = Guid.Parse("11111111-1111-1111-1111-111111111001");
        var othersRoadUserId = Guid.Parse("11111111-1111-1111-1111-111111111007");

        modelBuilder.Entity<ProfileCategory>().HasData(
            new ProfileCategory { CategoryId = commercialTruckDriverId, Name = "Commercial Truck Driver", SortOrder = 1, IsActive = true, IsOther = false },
            new ProfileCategory { CategoryId = othersRoadUserId, Name = "Others", SortOrder = 99, IsActive = true, IsOther = true }
        );

        // ============================================
        // PROFILE SUBCATEGORIES (Commercial Truck Driver Types)
        // ============================================
        var otrDriverId = Guid.Parse("22222222-2222-2222-2222-222222222001");
        var regionalDriverId = Guid.Parse("22222222-2222-2222-2222-222222222002");
        var localDeliveryDriverId = Guid.Parse("22222222-2222-2222-2222-222222222003");
        var localDriverId = Guid.Parse("22222222-2222-2222-2222-222222222004");
        var ownerOperatorId = Guid.Parse("22222222-2222-2222-2222-222222222005");
        var othersDriverTypeId = Guid.Parse("22222222-2222-2222-2222-222222222006");

        modelBuilder.Entity<ProfileSubCategory>().HasData(
            new ProfileSubCategory { SubCategoryId = otrDriverId, CategoryId = commercialTruckDriverId, Name = "OTR Driver (Over The Road)", SortOrder = 1, IsActive = true, IsOther = false },
            new ProfileSubCategory { SubCategoryId = regionalDriverId, CategoryId = commercialTruckDriverId, Name = "Regional Driver", SortOrder = 2, IsActive = true, IsOther = false },
            new ProfileSubCategory { SubCategoryId = localDeliveryDriverId, CategoryId = commercialTruckDriverId, Name = "Local Delivery Driver", SortOrder = 3, IsActive = true, IsOther = false },
            new ProfileSubCategory { SubCategoryId = localDriverId, CategoryId = commercialTruckDriverId, Name = "Local Driver", SortOrder = 4, IsActive = true, IsOther = false },
            new ProfileSubCategory { SubCategoryId = ownerOperatorId, CategoryId = commercialTruckDriverId, Name = "Owner-Operator", SortOrder = 5, IsActive = true, IsOther = false },
            new ProfileSubCategory { SubCategoryId = othersDriverTypeId, CategoryId = commercialTruckDriverId, Name = "Others", SortOrder = 99, IsActive = true, IsOther = true }
        );

        // ============================================
        // VEHICLE TYPES (Commercial Truck Types)
        // ============================================
        var truckTypes = new (Guid id, string name, int order, bool isOther)[]
        {
            (Guid.Parse("33333333-3333-3333-3333-333333333001"), "Bobtail", 1, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333002"), "Dry Van Semi-trailer Truck", 2, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333003"), "Reefer Semi-trailer Truck", 3, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333004"), "Standard Flatbed Truck", 4, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333005"), "Container Chassis Truck", 5, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333006"), "Drop-Deck/Step-Deck Trailer Truck", 6, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333007"), "Double Drop/Lowboy Trailer Truck", 7, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333008"), "Conestoga Trailer Truck", 8, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333009"), "Curtainside Trailer Truck", 9, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333010"), "Tanker Truck", 10, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333011"), "Car Hauler", 11, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333012"), "Livestock Truck", 12, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333013"), "Logging Truck", 13, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333014"), "Hot Shot Trailer Truck", 14, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333015"), "Pickup Truck", 15, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333016"), "Box Truck", 16, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333017"), "Dump Truck", 17, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333018"), "Garbage Truck", 18, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333019"), "Tow Truck", 19, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333020"), "Oversize Truck", 20, false),
            (Guid.Parse("33333333-3333-3333-3333-333333333099"), "Others", 99, true),
        };

        modelBuilder.Entity<VehicleType>().HasData(
            truckTypes.Select(t => new VehicleType
            {
                VehicleTypeId = t.id,
                SubCategoryId = null, // Can be used by any subcategory
                Name = t.name,
                SortOrder = t.order,
                IsActive = true,
                IsOther = t.isOther
            }).ToArray()
        );

        // ============================================
        // VEHICLE BRANDS (Truck Brands)
        // ============================================
        var truckBrands = new (Guid id, string name, int order, bool isOther)[]
        {
            (Guid.Parse("44444444-4444-4444-4444-444444444001"), "Freightliner", 1, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444002"), "Peterbilt", 2, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444003"), "Kenworth", 3, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444004"), "International", 4, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444005"), "Mack", 5, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444006"), "Western Star", 6, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444007"), "Dina", 7, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444008"), "Giant Motors", 8, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444009"), "Volkswagen", 9, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444010"), "Mercedes-Benz", 10, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444011"), "Renault", 11, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444012"), "Toyota", 12, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444013"), "Iveco", 13, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444014"), "Scania", 14, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444015"), "MAN", 15, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444016"), "DAF", 16, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444017"), "Unimog", 17, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444018"), "Astra", 18, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444019"), "Ginaf", 19, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444020"), "Dennis Eagle", 20, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444021"), "Alexander Dennis", 21, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444022"), "KamAZ", 22, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444023"), "Ural", 23, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444024"), "MAZ", 24, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444025"), "GAZ", 25, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444026"), "Toyota Hino", 26, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444027"), "Isuzu", 27, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444028"), "FAW Jiefang", 28, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444029"), "TATA Motors", 29, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444030"), "Mitsubishi", 30, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444031"), "Suzuki", 31, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444032"), "Fuso", 32, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444033"), "Ashok Leyland", 33, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444034"), "Dongfeng", 34, false),
            (Guid.Parse("44444444-4444-4444-4444-444444444099"), "Others", 99, true),
        };

        modelBuilder.Entity<VehicleBrand>().HasData(
            truckBrands.Select(b => new VehicleBrand
            {
                BrandId = b.id,
                VehicleTypeId = null, // Can be used by any vehicle type
                Name = b.name,
                SortOrder = b.order,
                IsActive = true,
                IsOther = b.isOther
            }).ToArray()
        );
    }
}
