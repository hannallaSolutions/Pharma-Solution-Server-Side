using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class drugAlternativeReport2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugAlternativeReports_Users_UserId",
                table: "DrugAlternativeReports");

            migrationBuilder.DropIndex(
                name: "IX_DrugAlternativeReports_UserId",
                table: "DrugAlternativeReports");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DrugAlternativeReports");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlternativeReports_ClassInfoId_SourceDrugNDC_TargetDrug~",
                table: "DrugAlternativeReports",
                columns: new[] { "ClassInfoId", "SourceDrugNDC", "TargetDrugNDC", "StatusDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlternativeReports_UserEmail",
                table: "DrugAlternativeReports",
                column: "UserEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugAlternativeReports_Users_UserEmail",
                table: "DrugAlternativeReports",
                column: "UserEmail",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugAlternativeReports_Users_UserEmail",
                table: "DrugAlternativeReports");

            migrationBuilder.DropIndex(
                name: "IX_DrugAlternativeReports_ClassInfoId_SourceDrugNDC_TargetDrug~",
                table: "DrugAlternativeReports");

            migrationBuilder.DropIndex(
                name: "IX_DrugAlternativeReports_UserEmail",
                table: "DrugAlternativeReports");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DrugAlternativeReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlternativeReports_UserId",
                table: "DrugAlternativeReports",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugAlternativeReports_Users_UserId",
                table: "DrugAlternativeReports",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
