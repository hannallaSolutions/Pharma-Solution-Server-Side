using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class switchtodrugdieseasehistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_DrugDiseases_DrugDiseaseId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserEmail",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropTable(
                name: "DrugDiseases");

            migrationBuilder.DropIndex(
                name: "IX_DrugDiseaseAddHistories_UserEmail",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.RenameColumn(
                name: "DrugDiseaseId",
                table: "DrugDiseaseAddHistories",
                newName: "DrugId");

            migrationBuilder.RenameIndex(
                name: "IX_DrugDiseaseAddHistories_DrugDiseaseId",
                table: "DrugDiseaseAddHistories",
                newName: "IX_DrugDiseaseAddHistories_DrugId");

            migrationBuilder.AddColumn<int>(
                name: "DiseaseId",
                table: "DrugDiseaseAddHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "DrugDiseaseAddHistories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DrugDiseaseAddHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "DrugDiseaseAddHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseAddHistories_DiseaseId",
                table: "DrugDiseaseAddHistories",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseAddHistories_UserId",
                table: "DrugDiseaseAddHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseAddHistories_UserId1",
                table: "DrugDiseaseAddHistories",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugDiseaseAddHistories_Diseases_DiseaseId",
                table: "DrugDiseaseAddHistories",
                column: "DiseaseId",
                principalTable: "Diseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugDiseaseAddHistories_Drugs_DrugId",
                table: "DrugDiseaseAddHistories",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Diseases_DiseaseId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Drugs_DrugId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserId1",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrugDiseaseAddHistories_DiseaseId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrugDiseaseAddHistories_UserId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrugDiseaseAddHistories_UserId1",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropColumn(
                name: "DiseaseId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "DrugDiseaseAddHistories");

            migrationBuilder.RenameColumn(
                name: "DrugId",
                table: "DrugDiseaseAddHistories",
                newName: "DrugDiseaseId");

            migrationBuilder.RenameIndex(
                name: "IX_DrugDiseaseAddHistories_DrugId",
                table: "DrugDiseaseAddHistories",
                newName: "IX_DrugDiseaseAddHistories_DrugDiseaseId");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "DrugDiseaseAddHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DrugDiseases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiseaseId = table.Column<int>(type: "integer", nullable: false),
                    DrugId = table.Column<int>(type: "integer", nullable: false),
                    userEmail = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Show = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugDiseases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugDiseases_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugDiseases_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugDiseases_Users_userEmail",
                        column: x => x.userEmail,
                        principalTable: "Users",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseAddHistories_UserEmail",
                table: "DrugDiseaseAddHistories",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseases_DiseaseId",
                table: "DrugDiseases",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseases_DrugId",
                table: "DrugDiseases",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseases_userEmail",
                table: "DrugDiseases",
                column: "userEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugDiseaseAddHistories_DrugDiseases_DrugDiseaseId",
                table: "DrugDiseaseAddHistories",
                column: "DrugDiseaseId",
                principalTable: "DrugDiseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugDiseaseAddHistories_Users_UserEmail",
                table: "DrugDiseaseAddHistories",
                column: "UserEmail",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
