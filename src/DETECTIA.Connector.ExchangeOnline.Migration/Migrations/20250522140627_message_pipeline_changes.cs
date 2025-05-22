using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class message_pipeline_changes : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScannedTime",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "ScannedAt");

            migrationBuilder.RenameColumn(
                name: "LastModifiedDateTime",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "ReceivedDateTime",
                schema: "DETECTIA",
                table: "Messages",
                newName: "ReceivedAt");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScannedAt",
                schema: "DETECTIA",
                table: "Messages",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScannedAt",
                schema: "DETECTIA",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "ScannedAt",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "ScannedTime");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                schema: "DETECTIA",
                table: "MessagesAttachements",
                newName: "LastModifiedDateTime");

            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                schema: "DETECTIA",
                table: "Messages",
                newName: "ReceivedDateTime");
        }
    }
}
