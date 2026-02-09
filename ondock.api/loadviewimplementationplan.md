# LoadView Feature Implementation Plan

> **Status: ✅ IMPLEMENTED** (Dec 22, 2025)

## Overview

Based on the UI designs, LoadView is a **location-based video request system** where users can request live or recorded video views of specific road locations from nearby truckers. This is distinct from regular chat - it's a broadcast request to nearby users who can respond with video content.

---

## UI Flow Analysis

### Row 1: Road Intelligence (Requester Flow)
1. **Map View** - User sees map with nearby truckers/points of interest
2. **Location Selection** - User taps on map to select location for LoadView request
3. **Request Creation** - Modal/sheet to create request with:
   - Location pin on map
   - Radius selection
   - Optional description/reason
4. **Countdown/Waiting** - Shows "3, 2, 1" countdown or waiting state
5. **Live Video Connection** - When responder accepts, shows live video stream
6. **Video Call Interface** - Full-screen video with controls (mute, end call, etc.)
7. **Response Gallery** - Multiple responses can come in as video thumbnails
8. **Response Details** - View individual response with video player

### Row 2: Road Posts (Responder Flow)
1. **Notification/Alert** - Responder sees incoming LoadView request notification
2. **Request Details** - Shows requester info, location, distance, urgency
3. **Accept/Decline** - Responder can accept or decline the request
4. **Camera View** - Opens camera to record/stream video response
5. **Recording Controls** - Record button, flip camera, etc.
6. **Submission** - Submit recorded video as response
7. **Confirmation** - Success state after submission

---

## Key Insights from Design

1. **NOT a traditional chat** - LoadView is a broadcast request system, not 1:1 messaging
2. **Real-time video** - Supports both live streaming AND recorded video responses
3. **Multiple responders** - One request can receive multiple video responses
4. **Location-centric** - Requests are tied to specific geographic locations
5. **Time-sensitive** - Requests have expiration (countdown visible in UI)
6. **Reward system** - Responders may earn rewards for helpful responses

---

## Revised Data Model

### LoadViewRequest (rename from RoadRequest)
```
- RequestId (PK)
- RequesterId (FK -> User)
- Location (geography Point)
- RadiusMeters (int)
- Description (string, optional)
- UrgencyLevel (enum: Low, Normal, High, Critical)
- Status (enum: Active, Fulfilled, Expired, Cancelled)
- CreatedAt
- ExpiresAt
- ResponseCount (denormalized for performance)
```

### LoadViewResponse (rename from RoadRequestResponse)
```
- ResponseId (PK)
- RequestId (FK -> LoadViewRequest)
- ResponderId (FK -> User)
- MediaUrl (string - video URL)
- ThumbnailUrl (string - video thumbnail)
- VideoDurationSeconds (int)
- Caption (string, optional)
- IsLiveStream (bool) - true if was live, false if recorded
- LiveKitRoomName (string, optional) - for live streams
- CreatedAt
- WasHelpful (bool, nullable) - requester feedback
- RewardPoints (int) - points earned for response
```

### Relationship to Chat
- **LoadView does NOT need ChatId** - it's a separate system
- Responses are NOT chat messages - they're video submissions
- However, a LoadView request could optionally trigger a notification in an existing chat if users are already connected

---

## API Endpoints

### Requester Endpoints
```
POST   /api/loadview/requests              - Create new LoadView request
GET    /api/loadview/requests              - Get user's own requests
GET    /api/loadview/requests/{id}         - Get specific request details
DELETE /api/loadview/requests/{id}         - Cancel a request
GET    /api/loadview/requests/{id}/responses - Get responses to a request
POST   /api/loadview/requests/{id}/feedback  - Rate a response as helpful
```

### Responder Endpoints
```
GET    /api/loadview/nearby                - Get nearby active requests
POST   /api/loadview/requests/{id}/respond - Submit video response
POST   /api/loadview/requests/{id}/live    - Start live stream response
DELETE /api/loadview/responses/{id}        - Delete own response
```

### Real-time (SignalR)
```
Hub: LoadViewHub
- JoinRequestRoom(requestId)     - Subscribe to request updates
- LeaveRequestRoom(requestId)    - Unsubscribe
- OnNewResponse(response)        - New response received
- OnRequestExpired(requestId)    - Request expired
- OnLiveStreamStarted(response)  - Live stream started
```

---

## Implementation Steps

### Phase 1: Entity & Database ✅
1. ✅ Keep RoadRequest/RoadRequestResponse entities (already exist)
2. ✅ Remove ChatId from RoadRequest (not needed)
3. ✅ Add ThumbnailUrl, IsLiveStream, LiveKitRoomName, RewardPoints to RoadRequestResponse
4. ✅ Add Description to RoadRequest
5. ✅ Create and apply migration

### Phase 2: DTOs ✅
1. ✅ CreateLoadViewRequestDto
2. ✅ LoadViewRequestDto
3. ✅ LoadViewResponseDto
4. ✅ NearbyLoadViewRequestsDto
5. ✅ SubmitLoadViewResponseDto
6. ✅ StartLiveResponseDto

### Phase 3: Service Layer ✅
1. ✅ ILoadViewService interface
2. ✅ LoadViewService implementation
   - CreateRequest with nearby user notification
   - GetNearbyRequests (spatial query)
   - SubmitResponse with realtime notification
   - StartLiveResponse (LiveKit integration)
   - EndLiveResponse
   - MarkResponseHelpful
   - ExpireOldRequests (background job)

### Phase 4: Controller ✅
1. ✅ LoadViewController with all endpoints

### Phase 5: Real-time ✅
1. ✅ LoadViewHub for SignalR (`/hubs/loadview`)
2. ✅ ILoadViewRealtimeService
3. ✅ SignalRLoadViewRealtimeService

### Phase 6: Background Services & Notifications ✅
1. ✅ LoadViewExpirationScanner - expires old requests
2. ✅ FCM push notifications for nearby requests, responses, live streams

---

## LiveKit Integration

For live video responses:
1. Responder calls `POST /api/loadview/requests/{id}/live`
2. Backend creates LiveKit room, returns token
3. Responder joins room and streams
4. Requester receives SignalR notification with room info
5. Requester joins same room to watch live
6. When stream ends, recording URL is saved to response

---

## Notification Flow

### For Requesters
- "Your LoadView request received a response!"
- "Live stream started for your request"

### For Potential Responders (nearby users)
- "Someone nearby is requesting a road view" (push notification)
- Show on map as special marker

---

## Priority Order

1. **Phase 1-2**: Database & DTOs (foundation)
2. **Phase 3-4**: Service & Controller (core functionality)
3. **Phase 5**: Real-time updates (enhanced UX)
4. **Phase 6**: Background jobs & notifications (polish)

---

## Clarifications (Dec 22, 2025)

### LoadView Request Flow (Push-Based)
When a LoadView request is created:
1. Backend queries `UserLocations` to find users within the request radius
2. Push notifications (FCM) are sent **directly** to nearby users
3. SignalR notifications also sent for real-time updates
4. `GetNearbyRequests` endpoint exists as fallback (if user missed notification)

### RoadPost Feature (User-Generated Content)
Separate from LoadView - users can post videos/photos to the map:
- `POST /api/roadposts` - Create a post
- `GET /api/roadposts/nearby` - Get nearby posts
- `GET /api/roadposts/mine` - Get user's own posts
- Posts show on map with user avatar

### Map Avatar Indicator
`NearbyUserDto` now includes:
- `PostCount` - Number of active posts by this user
- `HasRecentPost` - True if user posted in last 24 hours

---

## Questions/Decisions Needed

1. **Video storage**: Where are videos uploaded? (S3, Azure Blob, etc.)
2. **Live streaming**: Use existing LiveKit setup or separate rooms?
3. **Rewards**: How are reward points calculated and distributed?
4. **Rate limiting**: Max requests per user per day?
5. **Moderation**: How to handle inappropriate video content?
