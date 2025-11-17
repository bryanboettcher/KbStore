using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KbStore.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_InventoryStatusId",
                table: "Products",
                column: "InventoryStatusId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_InventoryStatusId",
                table: "Products");
        }
    }
}
