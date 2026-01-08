using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultProfilePicture : Migration
    {
        /// <inheritdoc />
         
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("UPDATE AspNetUsers SET ProfilePictureUrl = '/images/default-avatar.png' WHERE ProfilePictureUrl IS NULL");

			migrationBuilder.AlterColumn<string>(
		      name: "ProfilePictureUrl",
		      table: "AspNetUsers",
		      type: "nvarchar(max)",
		      nullable: false,
		      defaultValue: "/images/default-avatar.png",
		      oldClrType: typeof(string),
		      oldType: "nvarchar(max)",
		      oldNullable: true);
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
