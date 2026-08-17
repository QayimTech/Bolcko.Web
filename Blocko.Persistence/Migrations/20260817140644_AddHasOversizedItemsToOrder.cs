using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHasOversizedItemsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasOversizedItems",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasTruck",
                table: "DeliveryDrivers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TierLevel",
                table: "DeliveryDrivers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalDeliveredOrders",
                table: "DeliveryDrivers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsApiIntegration",
                table: "DeliveryCompanies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsOversized",
                table: "DeliveryCompanies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasOversizedItems",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HasTruck",
                table: "DeliveryDrivers");

            migrationBuilder.DropColumn(
                name: "TierLevel",
                table: "DeliveryDrivers");

            migrationBuilder.DropColumn(
                name: "TotalDeliveredOrders",
                table: "DeliveryDrivers");

            migrationBuilder.DropColumn(
                name: "IsApiIntegration",
                table: "DeliveryCompanies");

            migrationBuilder.DropColumn(
                name: "SupportsOversized",
                table: "DeliveryCompanies");
        }
    }
}
