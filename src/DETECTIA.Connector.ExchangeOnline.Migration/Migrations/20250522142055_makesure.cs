using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class makesure : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContainSensitive",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "IsSensitive");

            migrationBuilder.RenameColumn(
                name: "ContainSensitive",
                schema: "DETECTIA",
                table: "Messages",
                newName: "IsSensitive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsSensitive",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "ContainSensitive");

            migrationBuilder.RenameColumn(
                name: "IsSensitive",
                schema: "DETECTIA",
                table: "Messages",
                newName: "ContainSensitive");
        }
    }
}
