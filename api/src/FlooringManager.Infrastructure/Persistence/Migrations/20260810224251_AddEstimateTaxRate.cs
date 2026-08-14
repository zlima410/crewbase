using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlooringManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimateTaxRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "estimates",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "estimates");
        }
    }
}
