using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class fk_changes : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessagesDeltaLink",
                schema: "DETECTIA",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "MessagesDeltaLink",
                schema: "DETECTIA",
                table: "MailFolders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessagesDeltaLink",
                schema: "DETECTIA",
                table: "MailFolders");

            migrationBuilder.AddColumn<string>(
                name: "MessagesDeltaLink",
                schema: "DETECTIA",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
