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
                    DelegateMeetingMessageDeliveryOptions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailboxSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncStates",
                schema: "DETECTIA",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GivenName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPrincipalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MailNickname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OfficeLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MobilePhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessPhones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherMails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OnPremisesImmutableId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsageLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPasswordChangeDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserMailboxSettingsId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FoldersDeltaLink = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_MailboxSettings_UserMailboxSettingsId",
                        column: x => x.UserMailboxSettingsId,
                        principalSchema: "DETECTIA",
                        principalTable: "MailboxSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailFolders",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentFolderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChildFolderCount = table.Column<int>(type: "int", nullable: false),
                    TotalItemCount = table.Column<int>(type: "int", nullable: false),
                    UnreadItemCount = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModifiedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailFolders_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailFolders_UserId",
                schema: "DETECTIA",
                table: "MailFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserMailboxSettingsId",
                schema: "DETECTIA",
                table: "Users",
                column: "UserMailboxSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailFolders",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "SyncStates",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "MailboxSettings",
                schema: "DETECTIA");
        }
    }
}
