using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartphoneShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MasterUserId",
                table: "RepairRequests",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PurchaseOrders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_MasterUserId",
                table: "RepairRequests",
                column: "MasterUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairRequests_AspNetUsers_MasterUserId",
                table: "RepairRequests",
                column: "MasterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepairRequests_AspNetUsers_MasterUserId",
                table: "RepairRequests");

            migrationBuilder.DropIndex(
                name: "IX_RepairRequests_MasterUserId",
                table: "RepairRequests");

            migrationBuilder.DropColumn(
                name: "MasterUserId",
                table: "RepairRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PurchaseOrders",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
