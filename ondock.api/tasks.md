# OnDock Chat API Refactoring - Implementation Tasks

> **Status Legend:** ⬜ Not Started | 🔄 In Progress | ✅ Completed | ❌ Blocked

---

## Phase 1: Data Layer (Entities) ✅

### Profile Categorization

- [x] Create `ProfileCategory` entity
- [x] Create `ProfileSubCategory` entity
- [x] Create `VehicleType` entity
- [x] Create `VehicleBrand` entity

### Chat System

- [x] Create `Chat` entity (replacing `ChatRoom`)
- [x] Update `ChatParticipant` entity (RoomId → ChatId)
- [x] Update `Message` entity (RoomId → ChatId, add rich content fields)
- [x] Delete old `ChatRoom` entity

### User Profile

- [x] Update `UserProfile` entity (remove old enums, add FK references)
- [x] Remove old `DriverCategory` enum
- [x] Remove old `DriverSpecialization` enum

### Announcements

- [x] Create `Announcement` entity
- [x] Create `UrgencyLevel` enum (reusing from RoadPost)

### Identity & Encryption

- [x] Update `UserMatch` entity (add QR flow fields)
- [x] Create `MatchStatus` enum
- [x] Create `UserEncryptionKey` entity
- [x] Create `ChatKeyExchange` entity (replacing `RoomKeyExchange`)

---

## Phase 2: DTOs ✅

### Profile DTOs

- [x] Create `ProfileCategoryDto`
- [x] Create `ProfileSubCategoryDto`
- [x] Create `VehicleTypeDto`
- [x] Create `VehicleBrandDto`
- [x] Create `UpsertUserProfileRequest` (replacing `UpsertChatProfileRequest`)
- [x] Create `UserProfileResponse` (replacing `ChatProfileResponse`)
- [x] Delete old `UpsertChatProfileRequest`
- [x] Delete old `ChatProfileResponse`

### Chat DTOs

- [x] Create `CreateChatRequest`
- [x] Create `ChatResponse`
- [x] Create `ActiveChatDto` (updated existing)
- [x] Create `ChatParticipantDto`
- [x] Update `SendMessageRequest` (RoomId → ChatId)
- [x] Create `SendMediaMessageRequest`
- [x] Create `MessageContentType` enum (in Message entity)

### Announcement DTOs

- [x] Create `AnnouncementDto`
- [x] Create `AnnouncementListItemDto`

### Identity DTOs

- [x] Create `GenerateQRRequest`
- [x] Create `ScanQRRequest`
- [x] Create `QRCodeResponse`
- [x] Create `UserMatchDto`
- [x] Create `RevealedUserDto`

### Discovery DTOs

- [x] Update `NearbyUserDto` (add interest matching fields, RoomId → ChatId)

### Encryption DTOs

- [x] Create `RegisterKeyRequest`
- [x] Create `KeyExchangeRequest`
- [x] Create `PublicKeyResponse`

---

## Phase 3: Database Configuration ✅

### DbContext Updates

- [x] Add `DbSet<ProfileCategory>`
- [x] Add `DbSet<ProfileSubCategory>`
- [x] Add `DbSet<VehicleType>`
- [x] Add `DbSet<VehicleBrand>`
- [x] Add `DbSet<Chat>` (replace `DbSet<ChatRoom>`)
- [x] Add `DbSet<Announcement>`
- [x] Add `DbSet<UserEncryptionKey>`
- [x] Add `DbSet<ChatKeyExchange>`
- [x] Update entity configurations

### Migrations

- [ ] Create migration for new profile categorization tables
- [ ] Create migration for Chat table (replacing ChatRoom)
- [ ] Create migration for Announcement table
- [ ] Create migration for encryption tables
- [ ] Create migration for UserProfile FK changes
- [ ] Create migration for Message/ChatParticipant FK changes
- [ ] Create seed data migration

---

## Phase 4: Services ✅

### Profile Services

- [x] Create `IProfileCategoryService` interface
- [x] Implement `ProfileCategoryService`

### Chat Services

- [x] Update `IChatService` interface
- [x] Update `ChatService` implementation (complete refactor)
- [x] Update `IChatRealtimeService` interface (RoomEnded → ChatEnded)
- [x] Update `SignalRChatRealtimeService`

### Announcement Services

- [x] Create `IAnnouncementService` interface
- [x] Implement `AnnouncementService`

### Identity Services

- [x] Create `IIdentityService` interface
- [x] Implement `IdentityService`

### Encryption Services

- [x] Create `IEncryptionService` interface
- [x] Implement `EncryptionService`

### Background Services

- [x] Update `ChatRoomScanner` (now uses Chat instead of ChatRoom)

---

## Phase 5: Controllers ✅

### Profile Controllers

- [x] Create `CategoriesController`
- [x] Create `UsersController` (profile, DND, contacts, QR code endpoints)

### Chat Controllers

- [x] Update `ChatController` to use new DTOs
- [x] Create `ChatsController` (RESTful replacement)
- [x] Deprecate old `ChatController` endpoints (marked with [Obsolete])

### Announcement Controllers

- [x] Create `AnnouncementsController`

### Identity Controllers

- [x] Create `IdentityController`

### Encryption Controllers

- [x] Create `EncryptionController`

---

## Phase 6: SignalR ✅

- [x] Update `ChatHub` for new entities (ChatId)
- [x] Implement `ChatEnded` event (replacing `RoomEnded`)
- [x] Implement `AnnouncementReceived` event
- [x] Implement `IdentityRevealRequested` event
- [x] Implement `IdentityRevealed` event
- [x] Implement `ParticipantJoined` and `ParticipantLeft` events
- [x] Implement `TypingStarted` and `TypingStopped` events
- [x] Implement `ParticipantLocationUpdated` event
- [x] Implement `NearbyUsersUpdated` event

---

## Phase 7: Cleanup

### Remove Deprecated Code

- [x] Remove old `ChatRoom` entity
- [x] Remove old `RoomKeyExchange` entity (not present in codebase)
- [x] Remove old DTOs (`UpsertChatProfileRequest`, `ChatProfileResponse`)
- [x] Remove old controller endpoints (ChatController removed)
- [x] Remove old enums (`DriverCategory`, `DriverSpecialization`) from UserProfile

### Documentation

- [ ] Update API documentation
- [ ] Update Swagger annotations
- [ ] Document breaking changes

---

## Testing

- [ ] Unit tests for new services
- [ ] Integration tests for new endpoints
- [ ] Test database migrations
- [ ] Test SignalR events

---

## Notes

- **Breaking Changes**: RoomId → ChatId affects mobile clients
- **Migration Strategy**: Keep old endpoints during transition
- **Priority**: Focus on Phase 1-3 first (data layer foundation)

---

_Last Updated: December 20, 2024_

---

## Completed Summary (Dec 20, 2024)

**Build Status:** Successful (0 errors, 3 warnings)

### Key Changes Made:

1. **New Entities:** ProfileCategory, ProfileSubCategory, VehicleType, VehicleBrand, Chat, Announcement, UserEncryptionKey, ChatKeyExchange
2. **Updated Entities:** UserProfile (FK-based), ChatParticipant (ChatId), Message (ChatId + rich content), UserMatch (QR flow)
3. **New DTOs:** All profile categorization, identity, encryption, and announcement DTOs
4. **Refactored Services:** ChatService, ChatRoomScanner, ContactService, SimulationBackgroundService, MagicPushService
5. **Updated Controllers:** ChatController uses new DTOs
6. **Deleted:** ChatRoom entity, old profile DTOs

### Additional Changes (Session 2):

7. **New Services:** ProfileCategoryService, AnnouncementService, EncryptionService, IdentityService
8. **New Service Interfaces:** IProfileCategoryService, IAnnouncementService, IEncryptionService, IIdentityService
9. **New Controllers:** CategoriesController, AnnouncementsController, EncryptionController, IdentityController
10. **New DTOs:** KeyExchangeResponse, IdentityRevealRequest, IdentityRevealResponse, RevealedIdentityDto
11. **Service Registration:** All new services registered in ServiceExtensions.cs
12. **SignalR Updates:** IChatRealtimeService expanded with new events (ChatEnded, ParticipantJoined/Left, IdentityRevealRequested/Revealed, AnnouncementReceived, TypingStarted/Stopped, ParticipantLocationUpdated, NearbyUsersUpdated)
13. **SignalR Implementation:** SignalRChatRealtimeService updated to use Guid ChatId and implement all new events
14. **New Controller:** ChatsController (RESTful replacement for ChatController)
15. **Updated DTO:** LeaveChatRequest (RoomId → ChatId)
16. **New Controller:** UsersController (profile, DND, contacts, QR code endpoints)
17. **Deprecated:** ChatController marked with [Obsolete] attribute

---

_Last Updated: December 20, 2024_
