using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Ads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "Ads",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedOn",
                table: "Ads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Ads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ads_ReviewedByUserId",
                table: "Ads",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ads_AspNetUsers_ReviewedByUserId",
                table: "Ads",
                column: "ReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ads_AspNetUsers_ReviewedByUserId",
                table: "Ads");

            migrationBuilder.DropIndex(
                name: "IX_Ads_ReviewedByUserId",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "ReviewedOn",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Ads");
        }
    }
}
