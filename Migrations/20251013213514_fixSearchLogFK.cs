using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class fixSearchLogFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchLogs_InsuranceRxes_BinId",
                table: "SearchLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchLogs_Insurances_RxgroupId",
                table: "SearchLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchLogs_InsuranceRxes_RxgroupId",
                table: "SearchLogs",
                column: "RxgroupId",
                principalTable: "InsuranceRxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchLogs_Insurances_BinId",
                table: "SearchLogs",
                column: "BinId",
                principalTable: "Insurances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchLogs_InsuranceRxes_RxgroupId",
                table: "SearchLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchLogs_Insurances_BinId",
                table: "SearchLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchLogs_InsuranceRxes_BinId",
                table: "SearchLogs",
                column: "BinId",
                principalTable: "InsuranceRxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchLogs_Insurances_RxgroupId",
                table: "SearchLogs",
                column: "RxgroupId",
                principalTable: "Insurances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
