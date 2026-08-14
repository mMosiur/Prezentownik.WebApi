using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Prezentownik.WebApi.Models.Enums;

#nullable disable

namespace Prezentownik.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialApplicationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:app.itemType", "limited,limitless,singular");

            migrationBuilder.CreateTable(
                name: "GiftLists",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    OwnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Type = table.Column<ItemType>(type: "app.\"itemType\"", nullable: false),
                    TargetQuantity = table.Column<int>(type: "integer", nullable: true),
                    OrderNumber = table.Column<int>(type: "integer", nullable: false),
                    GiftListId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_GiftLists_GiftListId",
                        column: x => x.GiftListId,
                        principalSchema: "app",
                        principalTable: "GiftLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityClaimed = table.Column<int>(type: "integer", nullable: false),
                    ClaimerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RevocationToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Claims_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "app",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ItemId",
                schema: "app",
                table: "Claims",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_GiftListId",
                schema: "app",
                table: "Items",
                column: "GiftListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Claims",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Items",
                schema: "app");

            migrationBuilder.DropTable(
                name: "GiftLists",
                schema: "app");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:app.itemType", "limited,limitless,singular");
        }
    }
}
