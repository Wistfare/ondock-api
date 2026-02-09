# OnDock Chat Feature Refactoring Plan

## Executive Summary

This document outlines a comprehensive API redesign to address gaps between the current implementation and customer expectations for the **Advanced Anonymous Chat System**.

---

## Current State Analysis

### What Exists

| Feature                | Status         | Notes                                            |
| ---------------------- | -------------- | ------------------------------------------------ |
| Location-based chat    | ✅ Implemented | Uses 1-mile radius, 5 MPH threshold              |
| Anonymous usernames    | ✅ Implemented | `Driver-XXXXXX` format                           |
| Announcements          | ⚠️ Partial     | Stored in NotificationLogs, not dedicated entity |
| Profile categorization | ⚠️ Partial     | Only `DriverCategory` enum (4 options)           |
| DND mode               | ✅ Implemented | Stored in BatteryPreferences JSON                |
| Report system          | ✅ Implemented | Basic reporting via NotificationLogs             |
| QR identity reveal     | ⚠️ Partial     | `UserMatch` entity exists, no endpoints          |
| E2E encryption         | ⚠️ Partial     | Field exists, no key exchange                    |
| Auto-delete on exit    | ⚠️ Partial     | Time-based expiry only                           |

### Critical Gaps

1. **Hierarchical Profile Categorization** - Missing: Category → SubCategory → VehicleType → VehicleBrand
2. **Rich Message Types** - Missing: Image, Video, Audio, File content support
3. **QR Code Identity Matching** - Missing: Complete flow for scanning and revealing identity
4. **Interest-Based Matching** - Missing: Show common categories/interests between users
5. **Dedicated Announcement Entity** - Missing: Proper lifecycle management with radius persistence
6. **Real E2E Encryption Key Exchange** - Missing: Public key exchange mechanism
7. **Consistent Auto-Cleanup** - Missing: Immediate deletion when ANY participant exits range

---

## Proposed API Design

### 1. Profile Categorization System

#### New Entities

```csharp
// ProfileCategory.cs
public class ProfileCategory
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; }          // e.g., "Commercial Truck Driver"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

// ProfileSubCategory.cs
public class ProfileSubCategory
{
    public Guid SubCategoryId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; }          // e.g., "OTR Driver"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

// VehicleType.cs
public class VehicleType
{
    public Guid VehicleTypeId { get; set; }
    public Guid? SubCategoryId { get; set; }  // Nullable - some types are universal
    public string Name { get; set; }          // e.g., "Dry Van Semi-trailer"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

// VehicleBrand.cs
public class VehicleBrand
{
    public Guid BrandId { get; set; }
    public Guid? VehicleTypeId { get; set; }  // Nullable - brands span types
    public string Name { get; set; }          // e.g., "Freightliner"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
```

#### Updated UserProfile Entity

```csharp
public class UserProfile
{
    public Guid ProfileId { get; set; }
    public Guid UserId { get; set; }

    // Hierarchical categorization (foreign keys)
    public Guid CategoryId { get; set; }
    public Guid? SubCategoryId { get; set; }
    public Guid? VehicleTypeId { get; set; }
    public Guid? VehicleBrandId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### New Endpoints

```
GET  /api/categories                              → List all categories
GET  /api/categories/{categoryId}/subcategories   → List subcategories
GET  /api/vehicle-types?subCategoryId={id}        → List vehicle types
GET  /api/vehicle-brands?vehicleTypeId={id}       → List brands

POST /api/users/{userId}/profile                            → Create/update profile (EXISTING - enhance)
GET  /api/users/{userId}/profile                            → Get profile (EXISTING - enhance)
```

#### Enhanced Profile DTOs

```csharp
public class UpsertUserProfileRequest
{
    [Required]
    public Guid CategoryId { get; set; }
    public Guid? SubCategoryId { get; set; }
    public Guid? VehicleTypeId { get; set; }
    public Guid? VehicleBrandId { get; set; }
}

public class UserProfileResponse
{
    public bool HasProfile { get; set; }
    public ProfileCategoryDto? Category { get; set; }
    public ProfileSubCategoryDto? SubCategory { get; set; }
    public VehicleTypeDto? VehicleType { get; set; }
    public VehicleBrandDto? VehicleBrand { get; set; }
}
```

---

### 2. Announcement System Redesign

#### New Entity

```csharp
public class Announcement
{
    public Guid AnnouncementId { get; set; }
    public Guid AuthorId { get; set; }

    // Content
    public string Title { get; set; }
    public string Body { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }  // Low, Medium, High, Critical

    // Location context (where it was created)
    public Point OriginLocation { get; set; }
    public double RadiusMiles { get; set; } = 1;

    // Lifecycle
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }

    // Stats
    public int ViewCount { get; set; }
    public int ReplyCount { get; set; }
}

public enum UrgencyLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
```

#### New Endpoints

```
POST /api/announcements                         → Create announcement
GET  /api/announcements                         → List nearby active announcements
GET  /api/announcements/{id}                    → Get announcement details
DELETE /api/announcements/{id}                  → Delete own announcement
POST /api/announcements/{id}/reply              → Reply to announcement (creates chat)
GET  /api/announcements/my                      → List my announcements with reply counts
```

#### Announcement DTOs

```csharp
public class CreateAnnouncementRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Body { get; set; }

    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Medium;

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public double? RadiusMiles { get; set; }  // Defaults to 1 mile
}

public class AnnouncementDto
{
    public string AnnouncementId { get; set; }
    public string AuthorAnonymousId { get; set; }
    public string AuthorDisplayName { get; set; }
    public string AuthorDisplayColor { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public double DistanceMiles { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ReplyCount { get; set; }
    public bool IsMine { get; set; }
}
```

---

### 3. Chat Entity

#### New Entity

```csharp
public class Chat
{
    public Guid ChatId { get; set; }

    // Location context (where chat was initiated)
    public Point CenterLocation { get; set; }
    public int RadiusMeters { get; set; }        // Default ~1609 (1 mile)

    // Participants
    public int ActiveUserCount { get; set; }

    // Lifecycle
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public virtual ICollection<ChatParticipant> Participants { get; set; }
    public virtual ICollection<Message> Messages { get; set; }
}
```

#### Updated ChatParticipant Entity

```csharp
public class ChatParticipant
{
    public Guid ParticipantId { get; set; }
    public Guid ChatId { get; set; }              // FK to Chat
    public Guid UserId { get; set; }              // FK to User

    // Display
    public string DisplayColor { get; set; }

    // Location tracking
    public Point CurrentLocation { get; set; }
    public float CurrentSpeed { get; set; }
    public DateTime LastHeartbeat { get; set; }

    // Profile snapshot (for interest matching display)
    public Guid? CategoryId { get; set; }
    public Guid? SubCategoryId { get; set; }

    // E2E Encryption
    public string? PublicKey { get; set; }

    // Navigation
    public virtual Chat Chat { get; set; }
    public virtual User User { get; set; }
}
```

#### Chat Endpoints

```
GET  /api/chats                                 → List active chats for current user
GET  /api/chats/{chatId}                        → Get chat details
POST /api/chats                                 → Create/join chat with target user
DELETE /api/chats/{chatId}                      → Leave chat
GET  /api/chats/{chatId}/participants           → Get participants in chat
GET  /api/chats/{chatId}/history                → Get message history
```

#### Chat DTOs

```csharp
public class CreateChatRequest
{
    [Required]
    public string TargetAnonymousUserId { get; set; }

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public double SpeedMph { get; set; }
    public double? RadiusMiles { get; set; }
    public string? PublicKey { get; set; }        // For E2E encryption
    public Guid? AnnouncementId { get; set; }     // If replying to announcement
}

public class ChatResponse
{
    public Guid ChatId { get; set; }
    public string SelfTemporaryUserId { get; set; }
    public string SelfDisplayName { get; set; }
    public string SelfDisplayColor { get; set; }
    public string OtherTemporaryUserId { get; set; }
    public string OtherDisplayName { get; set; }
    public string OtherDisplayColor { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int CommonInterestsCount { get; set; }
    public List<string> CommonInterests { get; set; }
}

public class ActiveChatDto
{
    public Guid ChatId { get; set; }
    public string OtherAnonymousUserId { get; set; }
    public string OtherDisplayName { get; set; }
    public string OtherDisplayColor { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTimestamp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public double? DistanceMiles { get; set; }
    public bool CanMeet { get; set; }             // Within 50 meters
}
```

---

### 4. Rich Message Content Types

#### Updated Message Entity

```csharp
public class Message
{
    public Guid MessageId { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }

    // Content - only one should be populated
    public string? TextContent { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }

    // Encryption
    public string? EncryptedContent { get; set; }  // For E2E encrypted text
    public string? EncryptionKeyId { get; set; }   // Reference to key used

    // Metadata
    public MessageContentType ContentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }    // For threaded replies
    public Guid? AnnouncementId { get; set; }      // If replying to announcement

    public DateTime Timestamp { get; set; }
    public bool IsDeleted { get; set; }
}

public enum MessageContentType
{
    Text = 0,
    Image = 1,
    Video = 2,
    Audio = 3,
    File = 4,
    SystemNotice = 10
}
```

#### New Endpoints

```
POST /api/chats/{chatId}/message                          → Send text message (EXISTING - enhance)
POST /api/chats/{chatId}/message/media                    → Upload and send media message

GET  /api/chats/{chatId}/message/{id}/media               → Get media download URL
```

#### Message DTOs

```csharp
public class SendMessageRequest
{
    [Required]
    public Guid ChatId { get; set; }

    public string? EncryptedContent { get; set; }
    public MessageContentType ContentType { get; set; } = MessageContentType.Text;
    public Guid? ReplyToMessageId { get; set; }

    // Location for rule enforcement
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? SpeedMph { get; set; }
}

public class SendMediaMessageRequest
{
    [Required]
    public Guid ChatId { get; set; }

    [Required]
    public IFormFile File { get; set; }

    public MessageContentType ContentType { get; set; }
    public string? Caption { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? SpeedMph { get; set; }
}
```

---

### 5. QR Code Identity Reveal System

#### Updated UserMatch Entity

```csharp
public class UserMatch
{
    public Guid MatchId { get; set; }
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }

    // QR Code flow
    public string MatchCode { get; set; }         // Unique code for this match attempt
    public string? QRCodeHash { get; set; }       // Hash of scanned QR

    // State
    public MatchStatus Status { get; set; }
    public bool User1Revealed { get; set; }
    public bool User2Revealed { get; set; }

    // Chat linkage
    public Guid? ChatId { get; set; }           // The chat room where they met

    public DateTime InitiatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public enum MatchStatus
{
    Pending = 0,      // One user initiated
    Confirmed = 1,    // Both users scanned
    Expired = 2,
    Declined = 3
}
```

#### New Endpoints

```
POST /api/identity/qr/generate                  → Generate QR code for identity reveal
POST /api/identity/qr/scan                      → Scan another user's QR code
GET  /api/identity/matches                      → List confirmed identity matches
GET  /api/identity/pending                      → List pending match requests
POST /api/identity/reveal/{matchId}             → Confirm identity reveal (called after both users scanned each other's QR codes to finalize the mutual reveal)
DELETE /api/identity/match/{matchId}            → Decline/cancel match
```

#### Identity DTOs

```csharp
public class GenerateQRRequest
{
    [Required]
    public Guid ChatId { get; set; }           // Must have active chat

    [Required]
    public string TargetAnonymousUserId { get; set; }

    public int ExpiryMinutes { get; set; } = 5;
}

public class GenerateQRResponse
{
    public string MatchCode { get; set; }
    public string QRCodeData { get; set; }       // Base64 encoded QR image or data URL
    public DateTime ExpiresAt { get; set; }
}

public class ScanQRRequest
{
    [Required]
    public string QRCodeData { get; set; }       // Scanned QR content

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }
}

public class ScanQRResponse
{
    public string MatchId { get; set; }
    public MatchStatus Status { get; set; }
    public bool IdentityRevealed { get; set; }
    public RevealedUserDto? RevealedUser { get; set; }
}

public class RevealedUserDto
{
    public string UserId { get; set; }
    public string? DisplayName { get; set; }     // Real name if shared
    public string? Email { get; set; }           // If shared
    public string? Phone { get; set; }           // If shared
    public UserProfileResponse Profile { get; set; }
}
```

---

### 6. Interest-Based Matching & User Discovery

#### Enhanced Nearby Users Response

```csharp
public class NearbyUserDto
{
    public string AnonymousUserId { get; set; }
    public string DisplayName { get; set; }
    public string Color { get; set; }
    public double DistanceMiles { get; set; }
    public bool HasActiveChat { get; set; }
    public Guid? ChatId { get; set; }

    // NEW: Interest matching
    public int CommonInterestsCount { get; set; }
    public List<string> CommonInterests { get; set; }  // e.g., ["Commercial Truck", "OTR", "Freightliner"]

    // NEW: Last activity
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTimestamp { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
```

#### Interest Matching Logic

```csharp
private List<string> GetCommonInterests(UserProfile profile1, UserProfile profile2)
{
    var common = new List<string>();

    if (profile1.CategoryId == profile2.CategoryId)
        common.Add(profile1.Category.Name);

    if (profile1.SubCategoryId.HasValue && profile1.SubCategoryId == profile2.SubCategoryId)
        common.Add(profile1.SubCategory.Name);

    if (profile1.VehicleTypeId.HasValue && profile1.VehicleTypeId == profile2.VehicleTypeId)
        common.Add(profile1.VehicleType.Name);

    if (profile1.VehicleBrandId.HasValue && profile1.VehicleBrandId == profile2.VehicleBrandId)
        common.Add(profile1.VehicleBrand.Name);

    return common;
}
```

---

### 7. E2E Encryption Key Exchange

#### New Entity

```csharp
public class UserEncryptionKey
{
    public Guid KeyId { get; set; }
    public Guid UserId { get; set; }
    public string PublicKey { get; set; }        // RSA/ECDH public key (PEM format)
    public string KeyType { get; set; }          // "RSA-2048", "ECDH-P256"
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class RoomKeyExchange
{
    public Guid ExchangeId { get; set; }
    public Guid ChatId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public string EncryptedSessionKey { get; set; }  // Session key encrypted with recipient's public key
    public DateTime CreatedAt { get; set; }
}
```

#### New Endpoints

```
POST /api/encryption/keys                       → Register public key
GET  /api/encryption/keys/{userId}              → Get user's public key
POST /api/encryption/exchange                   → Exchange session key for room
GET  /api/encryption/exchange/{chatId}          → Get session keys for chat
```

---

### 8. Automatic Cleanup on Range Exit

#### Enhanced Chat Scanner Service

```csharp
public class ChatScanner : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ScanAndCleanupChatsAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ScanAndCleanupChatsAsync()
    {
        var activeChats = await _db.Chats
            .Where(c => c.IsActive && c.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var chat in activeChats)
        {
            var participants = await _db.ChatParticipants
                .Where(p => p.ChatId == chat.ChatId)
                .ToListAsync();

            var shouldEnd = false;
            var reason = "";

            foreach (var p in participants)
            {
                // Check speed threshold
                if (p.CurrentSpeed > _settings.SpeedThresholdMph)
                {
                    shouldEnd = true;
                    reason = "speed_threshold";
                    break;
                }

                // Check distance from chat center
                var distance = HaversineMeters(
                    p.CurrentLocation.Y, p.CurrentLocation.X,
                    chat.CenterLocation.Y, chat.CenterLocation.X);

                if (distance > chat.RadiusMeters)
                {
                    shouldEnd = true;
                    reason = "out_of_range";
                    break;
                }

                // Check heartbeat staleness (user went offline)
                if (p.LastHeartbeat < DateTime.UtcNow.AddMinutes(-2))
                {
                    shouldEnd = true;
                    reason = "participant_offline";
                    break;
                }
            }

            if (shouldEnd)
            {
                await EndChatAsync(chat.ChatId, reason);
            }
        }
    }

    private async Task EndChatAsync(Guid chatId, string reason)
    {
        // 1. Notify participants via SignalR
        await _realtime.SendChatEndedAsync(chatId, new { chatId, reason });

        // 2. Delete all messages
        var messages = await _db.Messages.Where(m => m.ChatId == chatId).ToListAsync();
        _db.Messages.RemoveRange(messages);

        // 3. Delete participants
        var participants = await _db.ChatParticipants.Where(p => p.ChatId == chatId).ToListAsync();
        _db.ChatParticipants.RemoveRange(participants);

        // 4. Delete chat
        var chat = await _db.Chats.FindAsync(chatId);
        if (chat != null)
            _db.Chats.Remove(chat);

        await _db.SaveChangesAsync();
    }
}
```

---

## Database Migration Plan

### Phase 1: New Tables

```sql
-- Profile categorization tables
CREATE TABLE ProfileCategories (
    CategoryId UUID PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE ProfileSubCategories (
    SubCategoryId UUID PRIMARY KEY,
    CategoryId UUID NOT NULL REFERENCES ProfileCategories(CategoryId),
    Name VARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE VehicleTypes (
    VehicleTypeId UUID PRIMARY KEY,
    SubCategoryId UUID REFERENCES ProfileSubCategories(SubCategoryId),
    Name VARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE VehicleBrands (
    BrandId UUID PRIMARY KEY,
    VehicleTypeId UUID REFERENCES VehicleTypes(VehicleTypeId),
    Name VARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT true
);

-- Chats (replacing ChatRooms)
CREATE TABLE Chats (
    ChatId UUID PRIMARY KEY,
    CenterLocation GEOGRAPHY(Point) NOT NULL,
    RadiusMeters INT NOT NULL DEFAULT 1609,
    ActiveUserCount INT NOT NULL DEFAULT 0,
    CreatedAt TIMESTAMP NOT NULL,
    ExpiresAt TIMESTAMP NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true
);

-- Announcements
CREATE TABLE Announcements (
    AnnouncementId UUID PRIMARY KEY,
    AuthorId UUID NOT NULL REFERENCES Users(Id),
    Title VARCHAR(200) NOT NULL,
    Body VARCHAR(2000) NOT NULL,
    UrgencyLevel INT NOT NULL DEFAULT 1,
    OriginLocation GEOGRAPHY(Point) NOT NULL,
    RadiusMiles FLOAT NOT NULL DEFAULT 1,
    CreatedAt TIMESTAMP NOT NULL,
    ExpiresAt TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    ViewCount INT NOT NULL DEFAULT 0,
    ReplyCount INT NOT NULL DEFAULT 0
);

-- Encryption
CREATE TABLE UserEncryptionKeys (
    KeyId UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES Users(Id),
    PublicKey TEXT NOT NULL,
    KeyType VARCHAR(50) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP NOT NULL,
    RevokedAt TIMESTAMP
);

CREATE TABLE ChatKeyExchanges (
    ExchangeId UUID PRIMARY KEY,
    ChatId UUID NOT NULL REFERENCES Chats(ChatId),
    FromUserId UUID NOT NULL REFERENCES Users(Id),
    ToUserId UUID NOT NULL REFERENCES Users(Id),
    EncryptedSessionKey TEXT NOT NULL,
    CreatedAt TIMESTAMP NOT NULL
);
```

### Phase 2: Alter Existing Tables

```sql
-- UserProfiles (replace old enum columns with FK references)
ALTER TABLE UserProfiles ADD CategoryId UUID REFERENCES ProfileCategories;
ALTER TABLE UserProfiles ADD SubCategoryId UUID REFERENCES ProfileSubCategories;
ALTER TABLE UserProfiles ADD VehicleTypeId UUID REFERENCES VehicleTypes;
ALTER TABLE UserProfiles ADD VehicleBrandId UUID REFERENCES VehicleBrands;
-- Drop old enum columns after migration
-- ALTER TABLE UserProfiles DROP COLUMN DriverCategory;
-- ALTER TABLE UserProfiles DROP COLUMN DriverSpecialization;

-- ChatParticipants (update to use ChatId instead of RoomId)
ALTER TABLE ChatParticipants ADD ChatId UUID REFERENCES Chats(ChatId);
ALTER TABLE ChatParticipants ADD CategoryId UUID REFERENCES ProfileCategories;
ALTER TABLE ChatParticipants ADD SubCategoryId UUID REFERENCES ProfileSubCategories;
-- After data migration: ALTER TABLE ChatParticipants DROP COLUMN RoomId;

-- Messages (update to use ChatId and add rich content support)
ALTER TABLE Messages ADD ChatId UUID REFERENCES Chats(ChatId);
ALTER TABLE Messages ADD TextContent TEXT;
ALTER TABLE Messages ADD ImageUrl VARCHAR(500);
ALTER TABLE Messages ADD VideoUrl VARCHAR(500);
ALTER TABLE Messages ADD AudioUrl VARCHAR(500);
ALTER TABLE Messages ADD FileUrl VARCHAR(500);
ALTER TABLE Messages ADD FileName VARCHAR(255);
ALTER TABLE Messages ADD FileSize BIGINT;
ALTER TABLE Messages ADD ContentType INT NOT NULL DEFAULT 0;
ALTER TABLE Messages ADD ReplyToMessageId UUID REFERENCES Messages(MessageId);
ALTER TABLE Messages ADD AnnouncementId UUID REFERENCES Announcements(AnnouncementId);
ALTER TABLE Messages ADD IsDeleted BOOLEAN NOT NULL DEFAULT false;
-- After data migration: ALTER TABLE Messages DROP COLUMN RoomId;

-- UserMatches (update for QR identity reveal)
ALTER TABLE UserMatches ADD MatchCode VARCHAR(50);
ALTER TABLE UserMatches ADD Status INT NOT NULL DEFAULT 0;
ALTER TABLE UserMatches ADD User1Revealed BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE UserMatches ADD User2Revealed BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE UserMatches ADD ChatId UUID REFERENCES Chats(ChatId);
ALTER TABLE UserMatches ADD InitiatedAt TIMESTAMP;
ALTER TABLE UserMatches ADD ConfirmedAt TIMESTAMP;
ALTER TABLE UserMatches ADD ExpiresAt TIMESTAMP;
```

### Phase 3: Seed Data

```sql
-- Insert default categories
INSERT INTO ProfileCategories (CategoryId, Name, SortOrder, IsActive) VALUES
    (gen_random_uuid(), 'Commercial Truck Driver', 1, true),
    (gen_random_uuid(), 'Bus Driver', 2, true),
    (gen_random_uuid(), 'Camper & RVer', 3, true),
    (gen_random_uuid(), 'Car Driver', 4, true),
    (gen_random_uuid(), 'Motorcyclist', 5, true),
    (gen_random_uuid(), 'Others', 99, true);

-- Insert subcategories for Commercial Truck
INSERT INTO ProfileSubCategories (SubCategoryId, CategoryId, Name, SortOrder, IsActive)
SELECT gen_random_uuid(), CategoryId, 'OTR Driver (Over The Road)', 1, true
FROM ProfileCategories WHERE Name = 'Commercial Truck Driver';

INSERT INTO ProfileSubCategories (SubCategoryId, CategoryId, Name, SortOrder, IsActive)
SELECT gen_random_uuid(), CategoryId, 'Regional Driver', 2, true
FROM ProfileCategories WHERE Name = 'Commercial Truck Driver';

INSERT INTO ProfileSubCategories (SubCategoryId, CategoryId, Name, SortOrder, IsActive)
SELECT gen_random_uuid(), CategoryId, 'Local Delivery Driver', 3, true
FROM ProfileCategories WHERE Name = 'Commercial Truck Driver';

INSERT INTO ProfileSubCategories (SubCategoryId, CategoryId, Name, SortOrder, IsActive)
SELECT gen_random_uuid(), CategoryId, 'Local Driver', 4, true
FROM ProfileCategories WHERE Name = 'Commercial Truck Driver';

INSERT INTO ProfileSubCategories (SubCategoryId, CategoryId, Name, SortOrder, IsActive)
SELECT gen_random_uuid(), CategoryId, 'Owner-Operator', 5, true
FROM ProfileCategories WHERE Name = 'Commercial Truck Driver';

INSERT INTO ProfileSubCategories (SubCategoryId, CategoryId, Name, SortOrder, IsActive)
SELECT gen_random_uuid(), CategoryId, 'Others', 99, true
FROM ProfileCategories WHERE Name = 'Commercial Truck Driver';

-- Insert vehicle types
INSERT INTO VehicleTypes (VehicleTypeId, SubCategoryId, Name, SortOrder, IsActive) VALUES
    (gen_random_uuid(), NULL, 'Bobtail', 1, true),
    (gen_random_uuid(), NULL, 'Dry Van Semi-trailer', 2, true),
    (gen_random_uuid(), NULL, 'Reefer Semi-trailer', 3, true),
    (gen_random_uuid(), NULL, 'Standard Flatbed', 4, true),
    (gen_random_uuid(), NULL, 'Container Chassis', 5, true),
    (gen_random_uuid(), NULL, 'Drop-Deck/Step-Deck Trailer', 6, true),
    (gen_random_uuid(), NULL, 'Double Drop/Lowboy Trailer', 7, true),
    (gen_random_uuid(), NULL, 'Conestoga Trailer', 8, true),
    (gen_random_uuid(), NULL, 'Curtainside Trailer', 9, true),
    (gen_random_uuid(), NULL, 'Tanker', 10, true),
    (gen_random_uuid(), NULL, 'Car Hauler', 11, true),
    (gen_random_uuid(), NULL, 'Livestock', 12, true),
    (gen_random_uuid(), NULL, 'Logging', 13, true),
    (gen_random_uuid(), NULL, 'Hot Shot Trailer', 14, true),
    (gen_random_uuid(), NULL, 'Pickup Truck', 15, true),
    (gen_random_uuid(), NULL, 'Box Truck', 16, true),
    (gen_random_uuid(), NULL, 'Dump Truck', 17, true),
    (gen_random_uuid(), NULL, 'Garbage Truck', 18, true),
    (gen_random_uuid(), NULL, 'Tow Truck', 19, true),
    (gen_random_uuid(), NULL, 'Oversize', 20, true),
    (gen_random_uuid(), NULL, 'Others', 99, true);

-- Insert vehicle brands
INSERT INTO VehicleBrands (BrandId, VehicleTypeId, Name, SortOrder, IsActive) VALUES
    (gen_random_uuid(), NULL, 'Freightliner', 1, true),
    (gen_random_uuid(), NULL, 'Peterbilt', 2, true),
    (gen_random_uuid(), NULL, 'Kenworth', 3, true),
    (gen_random_uuid(), NULL, 'International', 4, true),
    (gen_random_uuid(), NULL, 'Mack', 5, true),
    (gen_random_uuid(), NULL, 'Western Star', 6, true),
    (gen_random_uuid(), NULL, 'Volvo', 7, true),
    (gen_random_uuid(), NULL, 'Dina', 8, true),
    (gen_random_uuid(), NULL, 'Mercedes-Benz', 9, true),
    (gen_random_uuid(), NULL, 'Toyota Hino', 10, true),
    (gen_random_uuid(), NULL, 'Isuzu', 11, true),
    (gen_random_uuid(), NULL, 'TATA Motors', 12, true),
    (gen_random_uuid(), NULL, 'Scania', 13, true),
    (gen_random_uuid(), NULL, 'MAN', 14, true),
    (gen_random_uuid(), NULL, 'DAF', 15, true),
    (gen_random_uuid(), NULL, 'Iveco', 16, true),
    (gen_random_uuid(), NULL, 'Mitsubishi Fuso', 17, true),
    (gen_random_uuid(), NULL, 'Others', 99, true);
```

---

## API Endpoints Summary

### Profile & Categories

| Method | Endpoint                             | Description         |
| ------ | ------------------------------------ | ------------------- |
| GET    | `/api/categories`                    | List all categories |
| GET    | `/api/categories/{id}/subcategories` | List subcategories  |
| GET    | `/api/vehicle-types`                 | List vehicle types  |
| GET    | `/api/vehicle-brands`                | List brands         |
| GET    | `/api/users/{userId}/profile`        | Get user profile    |
| POST   | `/api/users/{userId}/profile`        | Update profile      |

### Nearby Users

| Method | Endpoint            | Description                             |
| ------ | ------------------- | --------------------------------------- |
| GET    | `/api/nearby-users` | Get nearby users with interest matching |

### Chats

| Method | Endpoint                           | Description           |
| ------ | ---------------------------------- | --------------------- |
| GET    | `/api/chats`                       | List active chats     |
| GET    | `/api/chats/{chatId}`              | Get chat details      |
| POST   | `/api/chats`                       | Create/join chat      |
| DELETE | `/api/chats/{chatId}`              | Leave chat            |
| GET    | `/api/chats/{chatId}/participants` | Get chat participants |
| GET    | `/api/chats/{chatId}/history`      | Get message history   |

### Messages

| Method | Endpoint                                  | Description        |
| ------ | ----------------------------------------- | ------------------ |
| POST   | `/api/chats/{chatId}/messages`            | Send text message  |
| POST   | `/api/chats/{chatId}/messages/media`      | Send media message |
| GET    | `/api/chats/{chatId}/messages/{id}/media` | Download media     |

### Announcements

| Method | Endpoint                        | Description               |
| ------ | ------------------------------- | ------------------------- |
| POST   | `/api/announcements`            | Create announcement       |
| GET    | `/api/announcements`            | List nearby announcements |
| GET    | `/api/announcements/{id}`       | Get announcement          |
| DELETE | `/api/announcements/{id}`       | Delete announcement       |
| POST   | `/api/announcements/{id}/reply` | Reply (creates chat)      |
| GET    | `/api/announcements/my`         | My announcements          |

### Identity Reveal

| Method | Endpoint                    | Description            |
| ------ | --------------------------- | ---------------------- |
| POST   | `/api/identity/qr/generate` | Generate QR for reveal |
| POST   | `/api/identity/qr/scan`     | Scan QR code           |
| GET    | `/api/identity/matches`     | List confirmed matches |
| GET    | `/api/identity/pending`     | List pending requests  |
| POST   | `/api/identity/reveal/{id}` | Confirm reveal         |
| DELETE | `/api/identity/match/{id}`  | Decline match          |

### Encryption

| Method | Endpoint                            | Description          |
| ------ | ----------------------------------- | -------------------- |
| POST   | `/api/encryption/keys`              | Register public key  |
| GET    | `/api/encryption/keys/{userId}`     | Get public key       |
| POST   | `/api/encryption/exchange`          | Exchange session key |
| GET    | `/api/encryption/exchange/{chatId}` | Get chat keys        |

### Settings

| Method | Endpoint                  | Description         |
| ------ | ------------------------- | ------------------- |
| GET    | `/api/users/{userId}/dnd` | Get DND status      |
| PUT    | `/api/users/{userId}/dnd` | Set DND status      |
| POST   | `/api/reports`            | Report user/message |

---

## Implementation Priority

### Phase 1 (High Priority - Core Experience)

1. ✨ **Hierarchical Profile Categorization** - Enables proper user matching
2. ✨ **Dedicated Announcement Entity** - Proper lifecycle management
3. ✨ **Interest-Based Matching** - Show common interests in nearby users

### Phase 2 (Medium Priority - Enhanced Features)

4. 🔐 **QR Code Identity Reveal** - Complete the mutual reveal flow
5. 📸 **Rich Message Types** - Image/Video/Audio/File support
6. 🧹 **Enhanced Auto-Cleanup** - Immediate deletion on range exit

### Phase 3 (Lower Priority - Security Hardening)

7. 🔒 **E2E Encryption Key Exchange** - True end-to-end encryption
8. 📊 **Analytics & Moderation** - Usage tracking, content moderation

---

## Breaking Changes

### Profile API Changes

- `UpsertChatProfileRequest.DriverCategory` → `CategoryId` (Guid)
- `UpsertChatProfileRequest.DriverSpecialization` → `SubCategoryId` (Guid)
- Add backward compatibility layer for transition period

### Message API Changes

- `SendMessageRequest.EncryptedContent` remains but `ContentType` added
- New `/media` endpoint for file uploads

### Announcement API Changes

- `/api/chat/announce` → `/api/announcements` (RESTful)
- Response includes proper `AnnouncementId` from entity

---

## SignalR Events

### Existing Events (renamed)

- `MessageReceived` - New message in chat
- `ChatEnded` - Chat session terminated (was `RoomEnded`)

### New Events

- `AnnouncementReceived` - New announcement in range
- `IdentityRevealRequested` - Someone wants to reveal identity
- `IdentityRevealed` - Identity successfully revealed
- `ParticipantLocationUpdated` - Participant moved (for UI updates)
- `NearbyUsersUpdated` - Nearby users list changed

---

## Next Steps

1. **Review this plan** with stakeholders
2. **Create database migrations** for new entities
3. **Implement Phase 1** endpoints
4. **Update mobile client** to use new APIs
5. **Deprecate legacy endpoints** with sunset period
6. **Implement Phase 2 & 3** based on feedback

---

_Document created: December 20, 2024_
_Author: Development Team_
