using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class GroupMembership : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserUserGroup",
                schema: "DETECTIA");

            migrationBuilder.AlterColumn<string>(
                name: "Pattern",
                schema: "DETECTIA",
                table: "Match",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "UserGroupMembership",
                schema: "DETECTIA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupMembership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroupMembership_UserGroups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "DETECTIA",
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroupMembership_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "DETECTIA",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Match_Pattern",
                schema: "DETECTIA",
                table: "Match",
                column: "Pattern");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMembership_GroupId",
                schema: "DETECTIA",
                table: "UserGroupMembership",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMembership_UserId",
                schema: "DETECTIA",
                table: "UserGroupMembership",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGroupMembership",
                schema: "DETECTIA");

            migrationBuilder.DropIndex(
                name: "IX_Match_Pattern",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.AlterColumn<string>(
                name: "Pattern",
                schema: "DETECTIA",
                table: "Match",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

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

            migrationBuilder.CreateIndex(
                name: "IX_UserUserGroup_UsersId",
                schema: "DETECTIA",
                table: "UserUserGroup",
                column: "UsersId");
        }
    }
}
