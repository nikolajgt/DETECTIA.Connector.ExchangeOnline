using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class attachments_flow : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessagesAttachements_Messages_UserMessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropIndex(
                name: "IX_MessagesAttachements_UserMessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropColumn(
                name: "ContentBytes",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropColumn(
                name: "UserMessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropColumn(
                name: "ExchangeUserId",
                schema: "DETECTIA",
                table: "MailFolders");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScannedTime",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagesAttachements_MessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_UserId",
                schema: "DETECTIA",
                table: "Messages",
                column: "UserId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_MessagesAttachements_Messages_MessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropIndex(
                name: "IX_MessagesAttachements_MessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropIndex(
                name: "IX_Messages_UserId",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ScannedTime",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.AddColumn<byte[]>(
                name: "ContentBytes",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserMessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExchangeUserId",
                schema: "DETECTIA",
                table: "MailFolders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_MessagesAttachements_UserMessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                column: "UserMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessagesAttachements_Messages_UserMessageId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                column: "UserMessageId",
                principalSchema: "DETECTIA",
                principalTable: "Messages",
                principalColumn: "Id");
        }
    }
}
