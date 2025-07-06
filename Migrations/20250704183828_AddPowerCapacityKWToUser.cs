using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarbonZero.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerCapacityKWToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PowerCapacityKW",
                table: "Users",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerCapacityKW",
                table: "Users");
        }
    }
}
