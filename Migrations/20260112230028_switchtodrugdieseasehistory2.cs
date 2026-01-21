using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class switchtodrugdieseasehistory2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId1",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrugDiseaseAddHistories_DrugId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrugDiseaseAddHistories_UserId1",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "DrugDiseaseAddHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseAddHistories_DrugId_DiseaseId_UserId",
                table: "DrugDiseaseAddHistories",
                columns: new[] { "DrugId", "DiseaseId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId",
                table: "DrugDiseaseAddHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrugDiseaseAddHistories_DrugId_DiseaseId_UserId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "DrugDiseaseAddHistories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "DrugDiseaseAddHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseAddHistories_DrugId",
                table: "DrugDiseaseAddHistories",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseAddHistories_UserId1",
                table: "DrugDiseaseAddHistories",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId",
                table: "DrugDiseaseAddHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId1",
                table: "DrugDiseaseAddHistories",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
