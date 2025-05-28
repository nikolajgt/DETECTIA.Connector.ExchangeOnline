using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DETECTIA.Connector.ExchangeOnline.Migration.Migrations
{
    /// <inheritdoc />
    public partial class renmaing_matches : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Match_EventAttachments_AttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_MessageAttachments_MessageMatch_AttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.RenameColumn(
                name: "MessageMatch_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "MessageAttachmentId");

            migrationBuilder.RenameColumn(
                name: "AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "EventAttachmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_MessageMatch_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_MessageAttachmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_EventAttachmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_EventAttachments_EventAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "EventAttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "EventAttachments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_MessageAttachments_MessageAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "MessageAttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "MessageAttachments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Match_EventAttachments_EventAttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_MessageAttachments_MessageAttachmentId",
                schema: "DETECTIA",
                table: "Match");

            migrationBuilder.RenameColumn(
                name: "MessageAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "MessageMatch_AttachmentId");

            migrationBuilder.RenameColumn(
                name: "EventAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "AttachmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_MessageAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_MessageMatch_AttachmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_EventAttachmentId",
                schema: "DETECTIA",
                table: "Match",
                newName: "IX_Match_AttachmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_EventAttachments_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "AttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "EventAttachments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Match_MessageAttachments_MessageMatch_AttachmentId",
                schema: "DETECTIA",
                table: "Match",
                column: "MessageMatch_AttachmentId",
                principalSchema: "DETECTIA",
                principalTable: "MessageAttachments",
                principalColumn: "Id");
        }
    }
}
