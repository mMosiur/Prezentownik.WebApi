using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prezentownik.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class Test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GiftLists_OwnerId",
                schema: "app",
                table: "GiftLists",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GiftLists_AspNetUsers_OwnerId",
                schema: "app",
                table: "GiftLists",
                column: "OwnerId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GiftLists_AspNetUsers_OwnerId",
                schema: "app",
                table: "GiftLists");

            migrationBuilder.DropIndex(
                name: "IX_GiftLists_OwnerId",
                schema: "app",
                table: "GiftLists");
        }
    }
}
