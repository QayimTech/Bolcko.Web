using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addoversizedprodctColmun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOversized",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Addresses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Addresses",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    ApiEmail = table.Column<string>(type: "text", nullable: false),
                    ApiPassword = table.Column<string>(type: "text", nullable: false),
                    WebhookSecret = table.Column<string>(type: "text", nullable: true),
                    OutboundWebhookUrl = table.Column<string>(type: "text", nullable: true),
                    CustomHeadersJson = table.Column<string>(type: "text", nullable: true),
                    CustomPayloadMappingJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryProviderConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryProviderLocationMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    SearchName = table.Column<string>(type: "text", nullable: false),
                    NormalizedSearchName = table.Column<string>(type: "text", nullable: true),
                    ExternalCityId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalCityName = table.Column<string>(type: "text", nullable: true),
                    ExternalRegionId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalRegionName = table.Column<string>(type: "text", nullable: true),
                    ExternalVillageId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalVillageName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryProviderLocationMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderShipmentMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ExternalPackageId = table.Column<string>(type: "text", nullable: true),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    CodBarcode = table.Column<string>(type: "text", nullable: true),
                    AwbPdfUrl = table.Column<string>(type: "text", nullable: true),
                    CurrentStatus = table.Column<string>(type: "text", nullable: true),
                    ArabicStatus = table.Column<string>(type: "text", nullable: true),
                    CodAmount = table.Column<double>(type: "double precision", nullable: false),
                    AssignedDriverName = table.Column<string>(type: "text", nullable: true),
                    AssignedDriverPhone = table.Column<string>(type: "text", nullable: true),
                    RawWebhookPayload = table.Column<string>(type: "text", nullable: true),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastStatusUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderShipmentMappings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryProviderConfigs");

            migrationBuilder.DropTable(
                name: "DeliveryProviderLocationMappings");

            migrationBuilder.DropTable(
                name: "OrderShipmentMappings");

            migrationBuilder.DropColumn(
                name: "IsOversized",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Addresses");
        }
    }
}
