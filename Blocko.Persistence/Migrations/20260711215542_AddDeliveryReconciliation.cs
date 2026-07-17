using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blocko.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CollectedAmount",
                table: "DeliveryJobs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryCompanyId",
                table: "DeliveryJobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReconciled",
                table: "DeliveryJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledAt",
                table: "DeliveryJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "DeliveryJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerUserId",
                table: "DeliveryCompanies",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryJobs_DeliveryCompanyId",
                table: "DeliveryJobs",
                column: "DeliveryCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryJobs_DeliveryCompanies_DeliveryCompanyId",
                table: "DeliveryJobs",
                column: "DeliveryCompanyId",
                principalTable: "DeliveryCompanies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryJobs_DeliveryCompanies_DeliveryCompanyId",
                table: "DeliveryJobs");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryJobs_DeliveryCompanyId",
                table: "DeliveryJobs");

            migrationBuilder.DropColumn(
                name: "CollectedAmount",
                table: "DeliveryJobs");

            migrationBuilder.DropColumn(
                name: "DeliveryCompanyId",
                table: "DeliveryJobs");

            migrationBuilder.DropColumn(
                name: "IsReconciled",
                table: "DeliveryJobs");

            migrationBuilder.DropColumn(
                name: "ReconciledAt",
                table: "DeliveryJobs");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "DeliveryJobs");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "DeliveryCompanies");
        }
    }
}
