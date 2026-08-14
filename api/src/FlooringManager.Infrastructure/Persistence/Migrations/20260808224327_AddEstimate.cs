using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlooringManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimateNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpirationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LaborSubtotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MaterialSubtotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "estimate_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LengthFeet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WidthFeet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WastePercentage = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    SquareFeet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BillableSquareFeet = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FlooringType = table.Column<int>(type: "integer", nullable: false),
                    WorkType = table.Column<int>(type: "integer", nullable: false),
                    LaborRatePerSqFt = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MaterialRatePerSqFt = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimate_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estimate_rooms_estimates_EstimateId",
                        column: x => x.EstimateId,
                        principalTable: "estimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_estimate_rooms_EstimateId_Position",
                table: "estimate_rooms",
                columns: new[] { "EstimateId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_estimates_CompanyId",
                table: "estimates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_estimates_CompanyId_EstimateNumber",
                table: "estimates",
                columns: new[] { "CompanyId", "EstimateNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_estimates_CompanyId_Status",
                table: "estimates",
                columns: new[] { "CompanyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estimate_rooms");

            migrationBuilder.DropTable(
                name: "estimates");
        }
    }
}
