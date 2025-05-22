using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class properties_name_changes : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailboxSettings_Users_ExchangeUserId",
                schema: "DETECTIA",
                table: "MailboxSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMessage_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessage");

            migrationBuilder.DropTable(
                name: "UserMessageAttachements",
                schema: "DETECTIA");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserMessage",
                schema: "DETECTIA",
                table: "UserMessage");

            migrationBuilder.RenameTable(
                name: "UserMessage",
                schema: "DETECTIA",
                newName: "Messages",
                newSchema: "DETECTIA");

            migrationBuilder.RenameColumn(
                name: "ExchangeUserId",
                schema: "DETECTIA",
                table: "Users",
                newName: "GraphId");

            migrationBuilder.RenameColumn(
                name: "FolderId",
                schema: "DETECTIA",
                table: "MailFolders",
                newName: "GraphId");

            migrationBuilder.RenameColumn(
                name: "ExchangeUserId",
                schema: "DETECTIA",
                table: "MailboxSettings",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MailboxSettings_ExchangeUserId",
                schema: "DETECTIA",
                table: "MailboxSettings",
                newName: "IX_MailboxSettings_UserId");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                schema: "DETECTIA",
                table: "Messages",
                newName: "GraphId");

            migrationBuilder.RenameIndex(
                name: "IX_UserMessage_UserMailFolderId",
                schema: "DETECTIA",
                table: "Messages",
                newName: "IX_Messages_UserMailFolderId");

            migrationBuilder.AddColumn<bool>(
                name: "ContainSensitive",
                schema: "DETECTIA",
                table: "Messages",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBeenScanned",
                schema: "DETECTIA",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                schema: "DETECTIA",
                table: "Messages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                schema: "DETECTIA",
                table: "Messages",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MessagesAttachements",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    GraphId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    IsInline = table.Column<bool>(type: "bit", nullable: false),
                    ContentBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    HasBeenScanned = table.Column<bool>(type: "bit", nullable: false),
                    ContainSensitive = table.Column<bool>(type: "bit", nullable: true),
                    LastModifiedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserMessageId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagesAttachements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagesAttachements_Messages_UserMessageId",
                        column: x => x.UserMessageId,
                        principalSchema: "DETECTIA",
                        principalTable: "Messages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessagesAttachements_UserMessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                column: "UserMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_MailboxSettings_Users_UserId",
                schema: "DETECTIA",
                table: "MailboxSettings",
                column: "UserId",
                principalSchema: "DETECTIA",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "Messages",
                column: "UserMailFolderId",
                principalSchema: "DETECTIA",
                principalTable: "MailFolders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailboxSettings_Users_UserId",
                schema: "DETECTIA",
                table: "MailboxSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "MessagesAttachements",
                schema: "DETECTIA");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ContainSensitive",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "HasBeenScanned",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.RenameTable(
                name: "Messages",
                schema: "DETECTIA",
                newName: "UserMessage",
                newSchema: "DETECTIA");

            migrationBuilder.RenameColumn(
                name: "GraphId",
                schema: "DETECTIA",
                table: "Users",
                newName: "ExchangeUserId");

            migrationBuilder.RenameColumn(
                name: "GraphId",
                schema: "DETECTIA",
                table: "MailFolders",
                newName: "FolderId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "DETECTIA",
                table: "MailboxSettings",
                newName: "ExchangeUserId");

            migrationBuilder.RenameIndex(
                name: "IX_MailboxSettings_UserId",
                schema: "DETECTIA",
                table: "MailboxSettings",
                newName: "IX_MailboxSettings_ExchangeUserId");

            migrationBuilder.RenameColumn(
                name: "GraphId",
                schema: "DETECTIA",
                table: "UserMessage",
                newName: "MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessage",
                newName: "IX_UserMessage_UserMailFolderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserMessage",
                schema: "DETECTIA",
                table: "UserMessage",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserMessageAttachements",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    ContentBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ContentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsInline = table.Column<bool>(type: "bit", nullable: false),
                    LastModifiedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_UserMessageAttachements_MessageId",
                schema: "DETECTIA",
                table: "UserMessageAttachements",
                column: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_MailboxSettings_Users_ExchangeUserId",
                schema: "DETECTIA",
                table: "MailboxSettings",
                column: "ExchangeUserId",
                principalSchema: "DETECTIA",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMessage_MailFolders_UserMailFolderId",
                schema: "DETECTIA",
                table: "UserMessage",
                column: "UserMailFolderId",
                principalSchema: "DETECTIA",
                principalTable: "MailFolders",
                principalColumn: "Id");
        }
    }
}
