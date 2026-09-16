using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancingTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancingTenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractorId = table.Column<int>(type: "integer", nullable: true),
                    ContractorName = table.Column<string>(type: "text", nullable: false),
                    ContractorPhone = table.Column<string>(type: "text", nullable: false),
                    ContractorCompany = table.Column<string>(type: "text", nullable: true),
                    TrackingCode = table.Column<string>(type: "text", nullable: false),
                    ProjectTitle = table.Column<string>(type: "text", nullable: false),
                    ProjectCity = table.Column<string>(type: "text", nullable: false),
                    ProjectAddress = table.Column<string>(type: "text", nullable: true),
                    BuildingPermitNumber = table.Column<string>(type: "text", nullable: false),
                    TargetLatitude = table.Column<double>(type: "double precision", nullable: false),
                    TargetLongitude = table.Column<double>(type: "double precision", nullable: false),
                    BaseMaterialCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ContractorMarkupRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalPayableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformAgencyFeeRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PlatformAgencyFeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvestorNetYieldAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TenureDays = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FunderInvestorId = table.Column<int>(type: "integer", nullable: true),
                    FunderName = table.Column<string>(type: "text", nullable: true),
                    FunderPhone = table.Column<string>(type: "text", nullable: true),
                    WakalaContractPdfUrl = table.Column<string>(type: "text", nullable: true),
                    PromissoryNoteUrl = table.Column<string>(type: "text", nullable: true),
                    IsCollateralVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ContractorTrustScore = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancingTenders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancingTenders_AspNetUsers_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancingTenders_AspNetUsers_FunderInvestorId",
                        column: x => x.FunderInvestorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancingTenderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancingTenderId = table.Column<int>(type: "integer", nullable: false),
                    MaterialCategory = table.Column<string>(type: "text", nullable: false),
                    MaterialName = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    UnitPriceJod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtotalJod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancingTenderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancingTenderItems_FinancingTenders_FinancingTenderId",
                        column: x => x.FinancingTenderId,
                        principalTable: "FinancingTenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobsiteProofOfDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancingTenderId = table.Column<int>(type: "integer", nullable: false),
                    DriverName = table.Column<string>(type: "text", nullable: false),
                    DriverPhone = table.Column<string>(type: "text", nullable: false),
                    VehiclePlateNumber = table.Column<string>(type: "text", nullable: false),
                    DriverLatitude = table.Column<double>(type: "double precision", nullable: false),
                    DriverLongitude = table.Column<double>(type: "double precision", nullable: false),
                    TargetJobsiteLatitude = table.Column<double>(type: "double precision", nullable: false),
                    TargetJobsiteLongitude = table.Column<double>(type: "double precision", nullable: false),
                    DistanceVarianceMeters = table.Column<double>(type: "double precision", nullable: false),
                    IsWithinGeoFence = table.Column<bool>(type: "boolean", nullable: false),
                    DispatcherOverride = table.Column<bool>(type: "boolean", nullable: false),
                    DispatcherOverrideNotes = table.Column<string>(type: "text", nullable: true),
                    PhotoEvidenceUrl = table.Column<string>(type: "text", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractorConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ContractorSignOffTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContractorDigitalSignatureUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobsiteProofOfDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobsiteProofOfDeliveries_FinancingTenders_FinancingTenderId",
                        column: x => x.FinancingTenderId,
                        principalTable: "FinancingTenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancingTenderItems_FinancingTenderId",
                table: "FinancingTenderItems",
                column: "FinancingTenderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingTenders_ContractorId",
                table: "FinancingTenders",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingTenders_FunderInvestorId",
                table: "FinancingTenders",
                column: "FunderInvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_JobsiteProofOfDeliveries_FinancingTenderId",
                table: "JobsiteProofOfDeliveries",
                column: "FinancingTenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobsiteProofOfDeliveries");

            migrationBuilder.DropTable(
                name: "FinancingTenderItems");

            migrationBuilder.DropTable(
                name: "FinancingTenders");
        }
    }
}
