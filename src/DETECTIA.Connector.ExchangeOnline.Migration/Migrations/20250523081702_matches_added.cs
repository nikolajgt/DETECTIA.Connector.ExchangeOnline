using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class matches_added : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastPasswordChangeDateTime",
                schema: "DETECTIA",
                table: "Users",
                newName: "LastPasswordChangeAt");

            migrationBuilder.RenameColumn(
                name: "CreatedDateTime",
                schema: "DETECTIA",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                schema: "DETECTIA",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "Messages",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "MailFolders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Match",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchCount = table.Column<int>(type: "int", nullable: false),
                    UserMessageAttachmentId = table.Column<long>(type: "bigint", nullable: true),
                    UserMessageId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Match", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Match_MessagesAttachements_UserMessageAttachmentId",
                        column: x => x.UserMessageAttachmentId,
                        principalSchema: "DETECTIA",
                        principalTable: "MessagesAttachements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Match_Messages_UserMessageId",
                        column: x => x.UserMessageId,
                        principalSchema: "DETECTIA",
                        principalTable: "Messages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserGroup",
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
                    table.PrimaryKey("PK_UserGroup", x => x.Id);
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
                        name: "FK_UserUserGroup_UserGroup_GroupsId",
                        column: x => x.GroupsId,
                        principalSchema: "DETECTIA",
                        principalTable: "UserGroup",
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
                name: "IX_MessagesAttachements_GraphId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_GraphId",
                schema: "DETECTIA",
                table: "Messages",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_MailFolders_GraphId",
                schema: "DETECTIA",
                table: "MailFolders",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_UserMessageAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "UserMessageAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_UserMessageId",
                schema: "DETECTIA",
                table: "Match",
                column: "UserMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroup_GraphId",
                schema: "DETECTIA",
                table: "UserGroup",
                column: "GraphId");

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
                name: "Match",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserUserGroup",
                schema: "DETECTIA");

            migrationBuilder.DropTable(
                name: "UserGroup",
                schema: "DETECTIA");

            migrationBuilder.DropIndex(
                name: "IX_Users_GraphId",
                schema: "DETECTIA",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Mail",
                schema: "DETECTIA",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_MessagesAttachements_GraphId",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.DropIndex(
                name: "IX_Messages_GraphId",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_MailFolders_GraphId",
                schema: "DETECTIA",
                table: "MailFolders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "DETECTIA",
                table: "MessagesAttachements");

            migrationBuilder.RenameColumn(
                name: "LastPasswordChangeAt",
                schema: "DETECTIA",
                table: "Users",
                newName: "LastPasswordChangeDateTime");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "DETECTIA",
                table: "Users",
                newName: "CreatedDateTime");

            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                schema: "DETECTIA",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                schema: "DETECTIA",
                table: "MailFolders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
