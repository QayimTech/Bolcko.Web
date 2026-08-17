using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityNameEnToShippingRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CityNameEn",
                table: "ShippingRates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityNameEn",
                table: "ShippingRates");
        }
    }
}
