using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class lastmig4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenderItems_Products_ProductId",
                table: "TenderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_AspNetUsers_UserId",
                table: "Tenders");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Tenders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "GuestCity",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestCompany",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestEmail",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "TenderItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "TenderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TenderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderItems_Products_ProductId",
                table: "TenderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_AspNetUsers_UserId",
                table: "Tenders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenderItems_Products_ProductId",
                table: "TenderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_AspNetUsers_UserId",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "GuestCity",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "GuestCompany",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "GuestEmail",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "TenderItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TenderItems");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Tenders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "TenderItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderItems_Products_ProductId",
                table: "TenderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_AspNetUsers_UserId",
                table: "Tenders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
