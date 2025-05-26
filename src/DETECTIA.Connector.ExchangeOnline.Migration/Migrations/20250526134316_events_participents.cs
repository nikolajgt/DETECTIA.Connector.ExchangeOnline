using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class events_participents : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "DETECTIA");

            migrationBuilder.CreateTable(
                name: "SyncStates",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GraphId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MailNickname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MailEnabled = table.Column<bool>(type: "bit", nullable: true),
                    SecurityEnabled = table.Column<bool>(type: "bit", nullable: true),
                    GroupTypes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Visibility = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GraphId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GivenName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mail = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserPrincipalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MailNickname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OfficeLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobilePhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessPhones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherMails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OnPremisesImmutableId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsageLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPasswordChangeAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FoldersDeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventsDeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GraphId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Start = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    End = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LocationDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: true),
                    ShowAs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserOrganizerId = table.Column<long>(type: "bigint", nullable: false),
                    Importance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Categories = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Users_UserOrganizerId",
                        column: x => x.UserOrganizerId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserMailboxSettings",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchiveFolder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutomaticRepliesEnabled = table.Column<bool>(type: "bit", nullable: true),
                    AutomaticRepliesInternalMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutomaticRepliesExternalMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingDays = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingHoursStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    WorkingHoursEndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    DelegateMeetingMessageDeliveryOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMailboxSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMailboxSettings_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMailFolders",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GraphId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentFolderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChildFolderCount = table.Column<int>(type: "int", nullable: false),
                    TotalItemCount = table.Column<int>(type: "int", nullable: false),
                    UnreadItemCount = table.Column<int>(type: "int", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MessagesDeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMailFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMailFolders_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserUserGroup",
                schema: "DETECTIA",
                columns: table => new
                {
                    GroupsId = table.Column<long>(type: "bigint", nullable: false),
                    UsersId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUserGroup", x => new { x.GroupsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserUserGroup_UserGroups_GroupsId",
                        column: x => x.GroupsId,
                        principalSchema: "DETECTIA",
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserUserGroup_Users_UsersId",
                        column: x => x.UsersId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventParticipants",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusResponse = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventParticipants_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "DETECTIA",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserMessages",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GraphId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FolderId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToRecipients = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    InternetMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasBeenScanned = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: true),
                    ScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserMailFolderId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMessages_UserMailFolders_UserMailFolderId",
                        column: x => x.UserMailFolderId,
                        principalSchema: "DETECTIA",
                        principalTable: "UserMailFolders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserMessages_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachments",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    GraphId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    IsInline = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    HasBeenScanned = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageAttachments_UserMessages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "DETECTIA",
                        principalTable: "UserMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchCount = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: true),
                    AttachmentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_MessageAttachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalSchema: "DETECTIA",
                        principalTable: "MessageAttachments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_UserMessages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "DETECTIA",
                        principalTable: "UserMessages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId",
                schema: "DETECTIA",
                table: "EventParticipants",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_UserId",
                schema: "DETECTIA",
                table: "EventParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UserOrganizerId",
                schema: "DETECTIA",
                table: "Events",
                column: "UserOrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AttachmentId",
                schema: "DETECTIA",
                table: "Matches",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MessageId",
                schema: "DETECTIA",
                table: "Matches",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_GraphId",
                schema: "DETECTIA",
                table: "MessageAttachments",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_MessageId",
                schema: "DETECTIA",
                table: "MessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GraphId",
                schema: "DETECTIA",
                table: "UserGroups",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMailboxSettings_UserId",
                schema: "DETECTIA",
                table: "UserMailboxSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMailFolders_GraphId",
                schema: "DETECTIA",
                table: "UserMailFolders",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMailFolders_UserId",
                schema: "DETECTIA",
                table: "UserMailFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_GraphId",
                schema: "DETECTIA",
                table: "UserMessages",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_UserId",
                schema: "DETECTIA",
                table: "UserMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessages",
                column: "UserMailFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GraphId",
                schema: "DETECTIA",
                table: "Users",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Mail",
                schema: "DETECTIA",
                table: "Users",
                column: "Mail");

            migrationBuilder.CreateIndex(
                name: "IX_UserUserGroup_UsersId",
                schema: "DETECTIA",
                table: "UserUserGroup",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventParticipants",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "Matches",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "SyncStates",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserMailboxSettings",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserUserGroup",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "MessageAttachments",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserGroups",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserMessages",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserMailFolders",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "DETECTIA");
        }
    }
}
