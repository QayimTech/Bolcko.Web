using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpressDeliveryToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExpressDelivery",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExpressDelivery",
                table: "Orders");
        }
    }
}
