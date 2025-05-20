using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class user_message_attachments : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "UserMessageAttachements",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserMessage",
                schema: "DETECTIA");
        }
    }
}
