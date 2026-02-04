using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLastLoginAddLastOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastLoginOn",
                table: "AspNetUsers",
                newName: "LastOnlineOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastOnlineOn",
                table: "AspNetUsers",
                newName: "LastLoginOn");
        }
    }
}
