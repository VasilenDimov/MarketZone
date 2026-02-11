using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLatitudeLongitude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Ads",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Ads",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Ads");
        }
    }
}
