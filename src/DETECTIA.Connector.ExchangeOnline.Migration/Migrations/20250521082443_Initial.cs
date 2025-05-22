using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Microsoft.EntityFrameworkCore.Migrations.Migration
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
                name: "Users",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExchangeUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GivenName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mail = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPasswordChangeDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FoldersDeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailboxSettings",
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
                    ExchangeUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailboxSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailboxSettings_Users_ExchangeUserId",
                        column: x => x.ExchangeUserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailFolders",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FolderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentFolderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChildFolderCount = table.Column<int>(type: "int", nullable: false),
                    TotalItemCount = table.Column<int>(type: "int", nullable: false),
                    UnreadItemCount = table.Column<int>(type: "int", nullable: false),
                    ExchangeUserId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MessagesDeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailFolders_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMessage",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FolderId = table.Column<long>(type: "bigint", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToRecipients = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    InternetMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserMailFolderId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMessage_MailFolders_UserMailFolderId",
                        column: x => x.UserMailFolderId,
                        principalSchema: "DETECTIA",
                        principalTable: "MailFolders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserMessageAttachements",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    IsInline = table.Column<bool>(type: "bit", nullable: false),
                    ContentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LastModifiedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessageAttachements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMessageAttachements_UserMessage_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "DETECTIA",
                        principalTable: "UserMessage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailboxSettings_ExchangeUserId",
                schema: "DETECTIA",
                table: "MailboxSettings",
                column: "ExchangeUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailFolders_UserId",
                schema: "DETECTIA",
                table: "MailFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessage_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessage",
                column: "UserMailFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessageAttachements_MessageId",
                schema: "DETECTIA",
                table: "UserMessageAttachements",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailboxSettings",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "SyncStates",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserMessageAttachements",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserMessage",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "MailFolders",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "DETECTIA");
        }
    }
}
