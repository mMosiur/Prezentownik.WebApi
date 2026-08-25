using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prezentownik.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticatedUserClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimerId",
                schema: "app",
                table: "Claims",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimerId",
                schema: "app",
                table: "Claims",
                column: "ClaimerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_AspNetUsers_ClaimerId",
                schema: "app",
                table: "Claims",
                column: "ClaimerId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_AspNetUsers_ClaimerId",
                schema: "app",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_ClaimerId",
                schema: "app",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ClaimerId",
                schema: "app",
                table: "Claims");
        }
    }
}
