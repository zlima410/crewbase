using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlooringManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomInstallationMethodAndFinishType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinishType",
                table: "estimate_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallationMethod",
                table: "estimate_rooms",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishType",
                table: "estimate_rooms");

            migrationBuilder.DropColumn(
                name: "InstallationMethod",
                table: "estimate_rooms");
        }
    }
}
