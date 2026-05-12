using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class scripttable1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AWP",
                table: "ScriptItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DaySupply",
                table: "ScriptItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DaySupplyEndDate",
                table: "ScriptItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossProfit",
                table: "ScriptItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Refill",
                table: "ScriptItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefillDate",
                table: "ScriptItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SDRA",
                table: "ScriptItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ScriptItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WAC",
                table: "ScriptItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DiseaseVisibilitySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 5, 11, 19, 42, 26, 461, DateTimeKind.Utc).AddTicks(8799));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AWP",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "DaySupply",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "DaySupplyEndDate",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "GrossProfit",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "Refill",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "RefillDate",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "SDRA",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ScriptItems");

            migrationBuilder.DropColumn(
                name: "WAC",
                table: "ScriptItems");

            migrationBuilder.UpdateData(
                table: "DiseaseVisibilitySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 5, 11, 19, 26, 2, 99, DateTimeKind.Utc).AddTicks(5746));
        }
    }
}
