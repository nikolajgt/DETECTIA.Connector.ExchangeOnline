using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class more_matches_logic : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Match_MessagesAttachements_UserMessageAttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_Messages_UserMessageId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_MessagesAttachements_Messages_MessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropForeignKey(
                name: "FK_UserUserGroup_UserGroup_GroupsId",
                schema: "DETECTIA",
                table: "UserUserGroup");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserGroup",
                schema: "DETECTIA",
                table: "UserGroup");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessagesAttachements",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Match",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.RenameTable(
                name: "UserGroup",
                schema: "DETECTIA",
                newName: "UserGroups",
                newSchema: "DETECTIA");

            migrationBuilder.RenameTable(
                name: "MessagesAttachements",
                schema: "DETECTIA",
                newName: "MessageAttachments",
                newSchema: "DETECTIA");

            migrationBuilder.RenameTable(
                name: "Messages",
                schema: "DETECTIA",
                newName: "UserMessages",
                newSchema: "DETECTIA");

            migrationBuilder.RenameTable(
                name: "Match",
                schema: "DETECTIA",
                newName: "Matches",
                newSchema: "DETECTIA");

            migrationBuilder.RenameColumn(
                name: "LastModifiedDateTime",
                schema: "DETECTIA",
                table: "MailFolders",
                newName: "LastModifiedAt");

            migrationBuilder.RenameIndex(
                name: "IX_UserGroup_GraphId",
                schema: "DETECTIA",
                table: "UserGroups",
                newName: "IX_UserGroups_GraphId");

            migrationBuilder.RenameIndex(
                name: "IX_MessagesAttachements_MessageId",
                schema: "DETECTIA",
                table: "MessageAttachments",
                newName: "IX_MessageAttachments_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_MessagesAttachements_GraphId",
                schema: "DETECTIA",
                table: "MessageAttachments",
                newName: "IX_MessageAttachments_GraphId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessages",
                newName: "IX_UserMessages_UserMailFolderId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_UserId",
                schema: "DETECTIA",
                table: "UserMessages",
                newName: "IX_UserMessages_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_GraphId",
                schema: "DETECTIA",
                table: "UserMessages",
                newName: "IX_UserMessages_GraphId");

            migrationBuilder.RenameColumn(
                name: "UserMessageId",
                schema: "DETECTIA",
                table: "Matches",
                newName: "MessageId");

            migrationBuilder.RenameColumn(
                name: "UserMessageAttachmentId",
                schema: "DETECTIA",
                table: "Matches",
                newName: "AttachmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_UserMessageId",
                schema: "DETECTIA",
                table: "Matches",
                newName: "IX_Matches_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_UserMessageAttachmentId",
                schema: "DETECTIA",
                table: "Matches",
                newName: "IX_Matches_AttachmentId");

            migrationBuilder.AddColumn<string>(
                name: "CalenderEventId",
                schema: "DETECTIA",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserGroups",
                schema: "DETECTIA",
                table: "UserGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageAttachments",
                schema: "DETECTIA",
                table: "MessageAttachments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserMessages",
                schema: "DETECTIA",
                table: "UserMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Matches",
                schema: "DETECTIA",
                table: "Matches",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CalenderEvents",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Start = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    End = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LocationDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: true),
                    ShowAs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyPreview = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizerEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Importance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Categories = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalenderEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventParticipant",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    GraphEventId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserId1 = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusResponse = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventParticipant_CalenderEvents_GraphEventId",
                        column: x => x.GraphEventId,
                        principalSchema: "DETECTIA",
                        principalTable: "CalenderEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventParticipant_Users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CalenderEventId",
                schema: "DETECTIA",
                table: "Users",
                column: "CalenderEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipant_GraphEventId",
                schema: "DETECTIA",
                table: "EventParticipant",
                column: "GraphEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipant_UserId1",
                schema: "DETECTIA",
                table: "EventParticipant",
                column: "UserId1");

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

            migrationBuilder.AddForeignKey(
                name: "FK_MessageAttachments_UserMessages_MessageId",
                schema: "DETECTIA",
                table: "MessageAttachments",
                column: "MessageId",
                principalSchema: "DETECTIA",
                principalTable: "UserMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMessages_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessages",
                column: "UserMailFolderId",
                principalSchema: "DETECTIA",
                principalTable: "MailFolders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMessages_Users_UserId",
                schema: "DETECTIA",
                table: "UserMessages",
                column: "UserId",
                principalSchema: "DETECTIA",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_CalenderEvents_CalenderEventId",
                schema: "DETECTIA",
                table: "Users",
                column: "CalenderEventId",
                principalSchema: "DETECTIA",
                principalTable: "CalenderEvents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserUserGroup_UserGroups_GroupsId",
                schema: "DETECTIA",
                table: "UserUserGroup",
                column: "GroupsId",
                principalSchema: "DETECTIA",
                principalTable: "UserGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_MessageAttachments_AttachmentId",
                schema: "DETECTIA",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_UserMessages_MessageId",
                schema: "DETECTIA",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageAttachments_UserMessages_MessageId",
                schema: "DETECTIA",
                table: "MessageAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMessages_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMessages_Users_UserId",
                schema: "DETECTIA",
                table: "UserMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_CalenderEvents_CalenderEventId",
                schema: "DETECTIA",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UserUserGroup_UserGroups_GroupsId",
                schema: "DETECTIA",
                table: "UserUserGroup");

            migrationBuilder.DropTable(
                name: "EventParticipant",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "CalenderEvents",
                schema: "DETECTIA");

            migrationBuilder.DropIndex(
                name: "IX_Users_CalenderEventId",
                schema: "DETECTIA",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserMessages",
                schema: "DETECTIA",
                table: "UserMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserGroups",
                schema: "DETECTIA",
                table: "UserGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageAttachments",
                schema: "DETECTIA",
                table: "MessageAttachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Matches",
                schema: "DETECTIA",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CalenderEventId",
                schema: "DETECTIA",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "UserMessages",
                schema: "DETECTIA",
                newName: "Messages",
                newSchema: "DETECTIA");

            migrationBuilder.RenameTable(
                name: "UserGroups",
                schema: "DETECTIA",
                newName: "UserGroup",
                newSchema: "DETECTIA");

            migrationBuilder.RenameTable(
                name: "MessageAttachments",
                schema: "DETECTIA",
                newName: "MessagesAttachements",
                newSchema: "DETECTIA");

            migrationBuilder.RenameTable(
                name: "Matches",
                schema: "DETECTIA",
                newName: "Match",
                newSchema: "DETECTIA");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                schema: "DETECTIA",
                table: "MailFolders",
                newName: "LastModifiedDateTime");

            migrationBuilder.RenameIndex(
                name: "IX_UserMessages_UserMailFolderId",
                schema: "DETECTIA",
                table: "Messages",
                newName: "IX_Messages_UserMailFolderId");

            migrationBuilder.RenameIndex(
                name: "IX_UserMessages_UserId",
                schema: "DETECTIA",
                table: "Messages",
                newName: "IX_Messages_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserMessages_GraphId",
                schema: "DETECTIA",
                table: "Messages",
                newName: "IX_Messages_GraphId");

            migrationBuilder.RenameIndex(
                name: "IX_UserGroups_GraphId",
                schema: "DETECTIA",
                table: "UserGroup",
                newName: "IX_UserGroup_GraphId");

            migrationBuilder.RenameIndex(
                name: "IX_MessageAttachments_MessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "IX_MessagesAttachements_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_MessageAttachments_GraphId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "IX_MessagesAttachements_GraphId");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                schema: "DETECTIA",
                table: "Match",
                newName: "UserMessageId");

            migrationBuilder.RenameColumn(
                name: "AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "UserMessageAttachmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_MessageId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_UserMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_UserMessageAttachmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                schema: "DETECTIA",
                table: "Messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserGroup",
                schema: "DETECTIA",
                table: "UserGroup",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessagesAttachements",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Match",
                schema: "DETECTIA",
                table: "Match",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_MessagesAttachements_UserMessageAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "UserMessageAttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "MessagesAttachements",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_Messages_UserMessageId",
                schema: "DETECTIA",
                table: "Match",
                column: "UserMessageId",
                principalSchema: "DETECTIA",
                principalTable: "Messages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "Messages",
                column: "UserMailFolderId",
                principalSchema: "DETECTIA",
                principalTable: "MailFolders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                schema: "DETECTIA",
                table: "Messages",
                column: "UserId",
                principalSchema: "DETECTIA",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagesAttachements_Messages_MessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                column: "MessageId",
                principalSchema: "DETECTIA",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserUserGroup_UserGroup_GroupsId",
                schema: "DETECTIA",
                table: "UserUserGroup",
                column: "GroupsId",
                principalSchema: "DETECTIA",
                principalTable: "UserGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
