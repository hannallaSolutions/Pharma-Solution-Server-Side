using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexForDrugWholesalerPrescriber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DrugWholesalerPrescribers_DrugId",
                table: "DrugWholesalerPrescribers");

            migrationBuilder.UpdateData(
                table: "DiseaseVisibilitySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 5, 17, 18, 54, 20, 238, DateTimeKind.Utc).AddTicks(5731));

            migrationBuilder.CreateIndex(
                name: "IX_DrugWholesalerPrescribers_DrugId_WholesalerId_PrescriberId_~",
                table: "DrugWholesalerPrescribers",
                columns: new[] { "DrugId", "WholesalerId", "PrescriberId", "PriceDate", "Price" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DrugWholesalerPrescribers_DrugId_WholesalerId_PrescriberId_~",
                table: "DrugWholesalerPrescribers");

            migrationBuilder.UpdateData(
                table: "DiseaseVisibilitySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 5, 11, 19, 42, 26, 461, DateTimeKind.Utc).AddTicks(8799));

            migrationBuilder.CreateIndex(
                name: "IX_DrugWholesalerPrescribers_DrugId",
                table: "DrugWholesalerPrescribers",
                column: "DrugId");
        }
    }
}
