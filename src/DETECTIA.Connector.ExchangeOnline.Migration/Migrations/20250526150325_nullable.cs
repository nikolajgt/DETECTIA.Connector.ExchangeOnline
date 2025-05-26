using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class nullable : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Users_UserOrganizerId",
                schema: "DETECTIA",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "UserOrganizerId",
                schema: "DETECTIA",
                table: "Events",
                newName: "OrganizerId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_UserOrganizerId",
                schema: "DETECTIA",
                table: "Events",
                newName: "IX_Events_OrganizerId");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ShowAs",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LocationDisplayName",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Importance",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BodyContentType",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Users_OrganizerId",
                schema: "DETECTIA",
                table: "Events",
                column: "OrganizerId",
                principalSchema: "DETECTIA",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Users_OrganizerId",
                schema: "DETECTIA",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "OrganizerId",
                schema: "DETECTIA",
                table: "Events",
                newName: "UserOrganizerId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_OrganizerId",
                schema: "DETECTIA",
                table: "Events",
                newName: "IX_Events_UserOrganizerId");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShowAs",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LocationDisplayName",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Importance",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BodyContentType",
                schema: "DETECTIA",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Users_UserOrganizerId",
                schema: "DETECTIA",
                table: "Events",
                column: "UserOrganizerId",
                principalSchema: "DETECTIA",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
