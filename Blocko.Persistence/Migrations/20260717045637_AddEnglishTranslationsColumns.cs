using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishTranslationsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Categories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Categories");
        }
    }
}
