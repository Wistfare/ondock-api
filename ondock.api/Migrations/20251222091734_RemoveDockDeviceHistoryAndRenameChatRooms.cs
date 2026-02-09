using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDockDeviceHistoryAndRenameChatRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatParticipants_ChatRooms_RoomId",
                table: "ChatParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_ChatRooms_RoomId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "ChatRooms");

            migrationBuilder.DropTable(
                name: "DockDevices");

            migrationBuilder.DropTable(
                name: "DockHistories");

            migrationBuilder.DropColumn(
                name: "BatteryPreferences",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DriverCategory",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DriverSpecialization",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TruckBrand",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TruckConfiguration",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TruckModel",
                table: "UserProfiles");

            migrationBuilder.RenameColumn(
                name: "MatchedAt",
                table: "UserMatches",
                newName: "InitiatedAt");

            migrationBuilder.RenameColumn(
                name: "IdentityRevealed",
                table: "UserMatches",
                newName: "User2Revealed");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "UserProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Preferences",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubCategoryId",
                table: "UserProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleBrandId",
                table: "UserProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleTypeId",
                table: "UserProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QRCodeHash",
                table: "UserMatches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "ChatId",
                table: "UserMatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "UserMatches",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UserMatches",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchCode",
                table: "UserMatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserMatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "User1Revealed",
                table: "UserMatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "MessageType",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "AnnouncementId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PublicKey",
                table: "ChatParticipants",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    AnnouncementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UrgencyLevel = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    OriginLocation = table.Column<Point>(type: "geography(Point)", nullable: false),
                    RadiusMiles = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    ReplyCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.AnnouncementId);
                    table.ForeignKey(
                        name: "FK_Announcements_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    ChatId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CenterLocation = table.Column<Point>(type: "geography(Point)", nullable: false),
                    Radius = table.Column<int>(type: "integer", nullable: false, defaultValue: 1609),
                    ActiveUserCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "ProfileCategories",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileCategories", x => x.CategoryId);
                });

            // Insert default category to satisfy foreign key constraint for existing UserProfiles
            migrationBuilder.Sql(@"
                INSERT INTO ""ProfileCategories"" (""CategoryId"", ""Name"", ""SortOrder"", ""IsActive"")
                VALUES ('00000000-0000-0000-0000-000000000000', 'Uncategorized', 0, true)
                ON CONFLICT (""CategoryId"") DO NOTHING;
            ");

            migrationBuilder.CreateTable(
                name: "UserEncryptionKeys",
                columns: table => new
                {
                    KeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    KeyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RevokedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEncryptionKeys", x => x.KeyId);
                    table.ForeignKey(
                        name: "FK_UserEncryptionKeys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatKeyExchanges",
                columns: table => new
                {
                    ExchangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedSessionKey = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatKeyExchanges", x => x.ExchangeId);
                    table.ForeignKey(
                        name: "FK_ChatKeyExchanges_AspNetUsers_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatKeyExchanges_AspNetUsers_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatKeyExchanges_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "ChatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileSubCategories",
                columns: table => new
                {
                    SubCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileSubCategories", x => x.SubCategoryId);
                    table.ForeignKey(
                        name: "FK_ProfileSubCategories_ProfileCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProfileCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleTypes",
                columns: table => new
                {
                    VehicleTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTypes", x => x.VehicleTypeId);
                    table.ForeignKey(
                        name: "FK_VehicleTypes_ProfileSubCategories_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "ProfileSubCategories",
                        principalColumn: "SubCategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VehicleBrands",
                columns: table => new
                {
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleBrands", x => x.BrandId);
                    table.ForeignKey(
                        name: "FK_VehicleBrands_VehicleTypes_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleTypes",
                        principalColumn: "VehicleTypeId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CategoryId",
                table: "UserProfiles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_SubCategoryId",
                table: "UserProfiles",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_VehicleBrandId",
                table: "UserProfiles",
                column: "VehicleBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_VehicleTypeId",
                table: "UserProfiles",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMatches_ChatId",
                table: "UserMatches",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMatches_MatchCode",
                table: "UserMatches",
                column: "MatchCode");

            migrationBuilder.CreateIndex(
                name: "IX_UserMatches_Status",
                table: "UserMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_AnnouncementId",
                table: "Messages",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_AuthorId",
                table: "Announcements",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CreatedAt_ExpiresAt",
                table: "Announcements",
                columns: new[] { "CreatedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsActive",
                table: "Announcements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_OriginLocation",
                table: "Announcements",
                column: "OriginLocation")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_ChatKeyExchanges_ChatId",
                table: "ChatKeyExchanges",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatKeyExchanges_FromUserId_ToUserId",
                table: "ChatKeyExchanges",
                columns: new[] { "FromUserId", "ToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatKeyExchanges_ToUserId",
                table: "ChatKeyExchanges",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_CenterLocation",
                table: "Chats",
                column: "CenterLocation")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ExpiresAt",
                table: "Chats",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileCategories_Name",
                table: "ProfileCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileSubCategories_CategoryId_Name",
                table: "ProfileSubCategories",
                columns: new[] { "CategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEncryptionKeys_UserId_IsActive",
                table: "UserEncryptionKeys",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBrands_Name",
                table: "VehicleBrands",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBrands_VehicleTypeId",
                table: "VehicleBrands",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_Name",
                table: "VehicleTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_SubCategoryId",
                table: "VehicleTypes",
                column: "SubCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatParticipants_Chats_RoomId",
                table: "ChatParticipants",
                column: "RoomId",
                principalTable: "Chats",
                principalColumn: "ChatId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Announcements_AnnouncementId",
                table: "Messages",
                column: "AnnouncementId",
                principalTable: "Announcements",
                principalColumn: "AnnouncementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chats_RoomId",
                table: "Messages",
                column: "RoomId",
                principalTable: "Chats",
                principalColumn: "ChatId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMatches_Chats_ChatId",
                table: "UserMatches",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "ChatId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_ProfileCategories_CategoryId",
                table: "UserProfiles",
                column: "CategoryId",
                principalTable: "ProfileCategories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_ProfileSubCategories_SubCategoryId",
                table: "UserProfiles",
                column: "SubCategoryId",
                principalTable: "ProfileSubCategories",
                principalColumn: "SubCategoryId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_VehicleBrands_VehicleBrandId",
                table: "UserProfiles",
                column: "VehicleBrandId",
                principalTable: "VehicleBrands",
                principalColumn: "BrandId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_VehicleTypes_VehicleTypeId",
                table: "UserProfiles",
                column: "VehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "VehicleTypeId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatParticipants_Chats_RoomId",
                table: "ChatParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Announcements_AnnouncementId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chats_RoomId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMatches_Chats_ChatId",
                table: "UserMatches");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_ProfileCategories_CategoryId",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_ProfileSubCategories_SubCategoryId",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_VehicleBrands_VehicleBrandId",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_VehicleTypes_VehicleTypeId",
                table: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "ChatKeyExchanges");

            migrationBuilder.DropTable(
                name: "UserEncryptionKeys");

            migrationBuilder.DropTable(
                name: "VehicleBrands");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "VehicleTypes");

            migrationBuilder.DropTable(
                name: "ProfileSubCategories");

            migrationBuilder.DropTable(
                name: "ProfileCategories");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_CategoryId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_SubCategoryId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_VehicleBrandId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_VehicleTypeId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserMatches_ChatId",
                table: "UserMatches");

            migrationBuilder.DropIndex(
                name: "IX_UserMatches_MatchCode",
                table: "UserMatches");

            migrationBuilder.DropIndex(
                name: "IX_UserMatches_Status",
                table: "UserMatches");

            migrationBuilder.DropIndex(
                name: "IX_Messages_AnnouncementId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Preferences",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SubCategoryId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VehicleBrandId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VehicleTypeId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "UserMatches");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "UserMatches");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UserMatches");

            migrationBuilder.DropColumn(
                name: "MatchCode",
                table: "UserMatches");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserMatches");

            migrationBuilder.DropColumn(
                name: "User1Revealed",
                table: "UserMatches");

            migrationBuilder.DropColumn(
                name: "AnnouncementId",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "User2Revealed",
                table: "UserMatches",
                newName: "IdentityRevealed");

            migrationBuilder.RenameColumn(
                name: "InitiatedAt",
                table: "UserMatches",
                newName: "MatchedAt");

            migrationBuilder.AddColumn<string>(
                name: "BatteryPreferences",
                table: "UserProfiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DriverCategory",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DriverSpecialization",
                table: "UserProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckBrand",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckConfiguration",
                table: "UserProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckModel",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QRCodeHash",
                table: "UserMatches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MessageType",
                table: "Messages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "PublicKey",
                table: "ChatParticipants",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ChatRooms",
                columns: table => new
                {
                    RoomId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActiveUserCount = table.Column<int>(type: "integer", nullable: false),
                    CenterLocation = table.Column<Point>(type: "geography(Point)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Radius = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatRooms", x => x.RoomId);
                });

            migrationBuilder.CreateTable(
                name: "DockDevices",
                columns: table => new
                {
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DockId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatteryLevel = table.Column<int>(type: "integer", nullable: false),
                    DeviceType = table.Column<int>(type: "integer", nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PairingCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockDevices", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_DockDevices_Docks_DockId",
                        column: x => x.DockId,
                        principalTable: "Docks",
                        principalColumn: "DockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DockHistories",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DockId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectionMethod = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_DockHistories_Docks_DockId",
                        column: x => x.DockId,
                        principalTable: "Docks",
                        principalColumn: "DockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_CenterLocation",
                table: "ChatRooms",
                column: "CenterLocation")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DockDevices_DockId",
                table: "DockDevices",
                column: "DockId");

            migrationBuilder.CreateIndex(
                name: "IX_DockDevices_PairingCode",
                table: "DockDevices",
                column: "PairingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DockHistories_DockId_Timestamp",
                table: "DockHistories",
                columns: new[] { "DockId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatParticipants_ChatRooms_RoomId",
                table: "ChatParticipants",
                column: "RoomId",
                principalTable: "ChatRooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_ChatRooms_RoomId",
                table: "Messages",
                column: "RoomId",
                principalTable: "ChatRooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
