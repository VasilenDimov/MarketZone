using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdTag");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Ads",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Ads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AdTags",
                columns: table => new
                {
                    AdId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdTags", x => new { x.AdId, x.TagId });
                    table.ForeignKey(
                        name: "FK_AdTags_Ads_AdId",
                        column: x => x.AdId,
                        principalTable: "Ads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdTags_TagId",
                table: "AdTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdTags");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Ads");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Ads",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 5000);

            migrationBuilder.CreateTable(
                name: "AdTag",
                columns: table => new
                {
                    AdsId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdTag", x => new { x.AdsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_AdTag_Ads_AdsId",
                        column: x => x.AdsId,
                        principalTable: "Ads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdTag_TagsId",
                table: "AdTag",
                column: "TagsId");
        }
    }
}
