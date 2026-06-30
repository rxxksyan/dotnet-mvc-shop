using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartphoneShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairSpareParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotesForClient",
                table: "RepairRequests",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ServicePrice",
                table: "RepairRequests",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepairSpareParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RepairRequestId = table.Column<int>(type: "int", nullable: false),
                    SparePartId = table.Column<int>(type: "int", nullable: true),
                    SparePartName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SparePartPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EstimatedWaitDays = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairSpareParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairSpareParts_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepairSpareParts_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSpareParts_RepairRequestId",
                table: "RepairSpareParts",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSpareParts_SparePartId",
                table: "RepairSpareParts",
                column: "SparePartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepairSpareParts");

            migrationBuilder.DropColumn(
                name: "NotesForClient",
                table: "RepairRequests");

            migrationBuilder.DropColumn(
                name: "ServicePrice",
                table: "RepairRequests");
        }
    }
}
