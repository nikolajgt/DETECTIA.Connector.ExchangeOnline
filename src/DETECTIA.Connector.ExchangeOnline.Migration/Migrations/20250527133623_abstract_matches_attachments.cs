using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class abstract_matches_attachments : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_MessageAttachments_AttachmentId",
                schema: "DETECTIA",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_UserMessages_MessageId",
                schema: "DETECTIA",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_EventParticipants_EventId",
                schema: "DETECTIA",
                table: "EventParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Matches",
                schema: "DETECTIA",
                table: "Matches");

            migrationBuilder.RenameTable(
                name: "Matches",
                schema: "DETECTIA",
                newName: "Match",
                newSchema: "DETECTIA");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_MessageId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_AttachmentId");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "HasBeenScanned",
                schema: "DETECTIA",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSensitive",
                schema: "DETECTIA",
                table: "Events",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScannedAt",
                schema: "DETECTIA",
                table: "Events",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                schema: "DETECTIA",
                table: "Match",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "EventAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Match",
                schema: "DETECTIA",
                table: "Match",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EventAttachments",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    CalendarEventId = table.Column<long>(type: "bigint", nullable: true),
                    GraphId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: true),
                    IsInline = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HasBeenScanned = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: true),
                    ScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAttachments_Events_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalSchema: "DETECTIA",
                        principalTable: "Events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_GraphId",
                schema: "DETECTIA",
                table: "Events",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId_UserId",
                schema: "DETECTIA",
                table: "EventParticipants",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Match_EventAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "EventAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttachments_CalendarEventId",
                schema: "DETECTIA",
                table: "EventAttachments",
                column: "CalendarEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_EventAttachments_EventAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "EventAttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "EventAttachments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_MessageAttachments_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "AttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "MessageAttachments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_UserMessages_MessageId",
                schema: "DETECTIA",
                table: "Match",
                column: "MessageId",
                principalSchema: "DETECTIA",
                principalTable: "UserMessages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Match_EventAttachments_EventAttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_MessageAttachments_AttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_UserMessages_MessageId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropTable(
                name: "EventAttachments",
                schema: "DETECTIA");

            migrationBuilder.DropIndex(
                name: "IX_Events_GraphId",
                schema: "DETECTIA",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_EventParticipants_EventId_UserId",
                schema: "DETECTIA",
                table: "EventParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Match",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropIndex(
                name: "IX_Match_EventAttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "HasBeenScanned",
                schema: "DETECTIA",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsSensitive",
                schema: "DETECTIA",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ScannedAt",
                schema: "DETECTIA",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "EventAttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.RenameTable(
                name: "Match",
                schema: "DETECTIA",
                newName: "Matches",
                newSchema: "DETECTIA");

            migrationBuilder.RenameIndex(
                name: "IX_Match_MessageId",
                schema: "DETECTIA",
                table: "Matches",
                newName: "IX_Matches_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_AttachmentId",
                schema: "DETECTIA",
                table: "Matches",
                newName: "IX_Matches_AttachmentId");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Matches",
                schema: "DETECTIA",
                table: "Matches",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId",
                schema: "DETECTIA",
                table: "EventParticipants",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_MessageAttachments_AttachmentId",
                schema: "DETECTIA",
                table: "Matches",
                column: "AttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "MessageAttachments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_UserMessages_MessageId",
                schema: "DETECTIA",
                table: "Matches",
                column: "MessageId",
                principalSchema: "DETECTIA",
                principalTable: "UserMessages",
                principalColumn: "Id");
        }
    }
}
