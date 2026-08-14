using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlooringManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimateRoomNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "estimate_rooms",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "estimate_rooms");
        }
    }
}
