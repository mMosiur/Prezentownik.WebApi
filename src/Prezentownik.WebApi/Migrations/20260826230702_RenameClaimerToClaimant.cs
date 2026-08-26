using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prezentownik.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameClaimerToClaimant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_AspNetUsers_ClaimerId",
                schema: "app",
                table: "Claims");

            migrationBuilder.RenameColumn(
                name: "ClaimerName",
                schema: "app",
                table: "Claims",
                newName: "ClaimantName");

            migrationBuilder.RenameColumn(
                name: "ClaimerId",
                schema: "app",
                table: "Claims",
                newName: "ClaimantId");

            migrationBuilder.RenameIndex(
                name: "IX_Claims_ClaimerId",
                schema: "app",
                table: "Claims",
                newName: "IX_Claims_ClaimantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_AspNetUsers_ClaimantId",
                schema: "app",
                table: "Claims",
                column: "ClaimantId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_AspNetUsers_ClaimantId",
                schema: "app",
                table: "Claims");

            migrationBuilder.RenameColumn(
                name: "ClaimantName",
                schema: "app",
                table: "Claims",
                newName: "ClaimerName");

            migrationBuilder.RenameColumn(
                name: "ClaimantId",
                schema: "app",
                table: "Claims",
                newName: "ClaimerId");

            migrationBuilder.RenameIndex(
                name: "IX_Claims_ClaimantId",
                schema: "app",
                table: "Claims",
                newName: "IX_Claims_ClaimerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_AspNetUsers_ClaimerId",
                schema: "app",
                table: "Claims",
                column: "ClaimerId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
