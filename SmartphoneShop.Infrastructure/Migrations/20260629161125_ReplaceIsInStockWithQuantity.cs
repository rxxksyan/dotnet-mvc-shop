using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartphoneShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsInStockWithQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Smartphones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Smartphones SET Quantity = 10 WHERE IsInStock = 1");
            migrationBuilder.Sql("UPDATE Smartphones SET Quantity = 0 WHERE IsInStock = 0");

            migrationBuilder.DropColumn(
                name: "IsInStock",
                table: "Smartphones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInStock",
                table: "Smartphones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE Smartphones SET IsInStock = 1 WHERE Quantity > 0");
            migrationBuilder.Sql("UPDATE Smartphones SET IsInStock = 0 WHERE Quantity = 0");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Smartphones");
        }
    }
}
