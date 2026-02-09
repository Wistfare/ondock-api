VehicleBrand
* VehicleTypeId


Profile
* UserId
* ProfileCategoryId
* ProfileCategorySubCategoryId
* VehicleTypeId
* VehicleBrandId


Announcement
* Title
* Body
* UrgencyLevel
* UserId

Chat
* Messages
* Participants

Message
* TextContent
* ImageContent
* VideoContent
* AudioContent
* FileContent
* SenderId
* ChatId
* AnnouncementId

Current Understanding of the feature
-------------------------------------


So we're about to implement profile categorization in OnDock project. So the feature of profile categorization starts in this manner. A user is going to have a main category, a subcategory, and then each subcategory is going to consist of track types. And then from a track type, you get to have...

Now, from the track type, and then from the track, I mean, from subcategory, you get to have track type and track brand. The user should be able to specify either track type or brandin case the track type or the model is not present. One is on the feature of profile categorization. And then from here, we move on to the feature of announcement. A user should be able to create announcements of his choice. The announcement should appear in the board of currently present announcement. Announcements should only be visible to users within one radius range.

And then from announcements, user should be able to chat with each other anonymously without revealing identity. User can also view the announcements of the posted user in their chat box or in their chat inbox. Now, four users should be able to view each other's chat strictly only if they're within one mile radius range or they're not moving beyond five mile per hour, but within one mile radius range. And the user should be able to reveal identity with each other in case they decide to do so through a QR code that a user can scan and then the anonymous username disappears.

Now, they should, the user should also view other users within the chatting range. They should be able to view the number of categories that they have in common or the number of items that they have in common on the chat for a user to be able to match with each other.


requirements given by the client
--------------------------------

3.2 ADVANCED ANONYMOUS CHAT SYSTEM
3.2.1 Core Functionality
Purpose: Location-based anonymous communication system enabling drivers to connect and share information while maintaining privacy. Note on this feature every chat session gets deleted when the one of the drivers gets out of the active range (not moving above 5mph and in a radius less than 1 mile.
Key Features:

Here users should be displayed in array of colors expect for green and red. Green users are nearby users that we don’t have chat with. 
As a truck driver I should be able to send end to end encrypted messages within my active chat sessions.
As a truck driver while using the app my username should be random and not visible to any other users.
Location-based chat rooms with 0-1 mile configurable radius. Users are sorted by distance (nearest first) and automatically removed when they exceed the set range or travel faster than 5 MPH. Includes community profile filtering and random color assignment (excluding red and blue). 
Profile categorization system or community categorization system (Truck Driver, Bus Driver, Regular Driver, RV/Camping Enthusiast)
User story: As the truck driver I need to be able to choose my driving category to be part of the community, truck brand, and model as part of onboarding.
Interest-based user matching with optional identity reveal (QR code matching between users)
User story: as truck drivers when we choose to reveal our identity we can do so by scanning each other's qr code. Then in chat we get to know each other instead of the default anonymous behavior but our chat should remain provided that we maintain an active chat range. 
Announcement broadcasting to nearby drivers 
User story: As a truck driver I should be able to post an announcement to the entire members within the chat range and users should respond to the announcement in my inbox. If one of the users decides to do so. 
Do Not Disturb mode with privacy controls
End-to-end encryption for all messages
Automated message deletion (clean up as soon as you are out of set radius)
Content moderation and reporting system
3.2.2 Profile Categorization System
Main Categories:
Commercial Truck Driver
Bus/Transit Driver
Regular Vehicle Driver
RV/Camping Enthusiast
Sub-Categories (Truck Drivers):
Long-haul/OTR
Local/Regional
Owner-Operator
Fleet Driver
Specialized Transport (Hazmat, Oversized, etc.)
Vehicle Type Matching:
Truck models and configurations
Preferred routes and destinations
Experience levels and certifications
Cargo types and specializations


Sample data
----------------

Ondock categories

 Road users:

* 🚛Commercial truck drivers
* 🚐bus drivers(to be developed)
* ⛺️camper&RVer(to be developed)
* 🚗car driver(to be developed)
* 🛵motocyclist(to be developed)
* 🚶‍♂️walker(to be developed)
* Others


🔽
 


commercial truck driver types

* OTR Driver– (Over The Road) 
* Regional Driver
* Local Delivery Driver
* Local driver
* Owner-Operator
* Others


🔽


Commercial Truck types

* Bobtail
* Dry Van,Semi-trailer truck
* Reefer Semi-trailer truck
* Standard Flatbed truck
* Container Chassis truck
* Drop-Deck/Step-Deck Trailer truck
* Double Drop/Lowboy Trailer truck
* Conestoga Trailer truck
* Curtainside trailer truck
* Tanker truck
* Car hauler
* Livestock truck
* Logging truck 
* Hot shot trailer truck
* Pickup truck 
* Box truck 
* Dump truck 
* Garbage truck
* Tow truck
* oversize truck
* Others



⬇️


Truck Brand

* Freightliner
* Peterbilt
* Kenworth
* International
* Mack
* Western Star
* Dina
* Giant Motors
* Volkswagen
* Mercedes-Benz 
* Renault
* TOYOTA
* Iveco
* Scania
* MAN
* DAF
* Unimog
* Astra
* Ginaf
* Dennis Eagle 
* Alexander Dennis
* KamAZ
* Ural
* MAZ
* GAZ
* Toyota Hino
* Isuzu
* FAW Jiefang
* TATA motors
* Mitsubishi
* Suzuki
* Fuso
* Ashok Leyland
* Dongfeng
* Others