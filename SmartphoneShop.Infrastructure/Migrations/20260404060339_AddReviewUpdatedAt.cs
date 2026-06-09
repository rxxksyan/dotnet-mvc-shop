using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartphoneShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Reviews",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Reviews");
        }
    }
}
