using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class drugAlternativeReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DrugAlternativeStatuses",
                columns: table => new
                {
                    SourceDrugNDC = table.Column<string>(type: "text", nullable: false),
                    TargetDrugNDC = table.Column<string>(type: "text", nullable: false),
                    ClassInfoId = table.Column<int>(type: "integer", nullable: false),
                    ApprovedStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugAlternativeStatuses", x => new { x.SourceDrugNDC, x.TargetDrugNDC, x.ClassInfoId });
                    table.ForeignKey(
                        name: "FK_DrugAlternativeStatuses_ClassInfos_ClassInfoId",
                        column: x => x.ClassInfoId,
                        principalTable: "ClassInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugAlternativeStatuses_Drugs_SourceDrugNDC",
                        column: x => x.SourceDrugNDC,
                        principalTable: "Drugs",
                        principalColumn: "NDC",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugAlternativeStatuses_Drugs_TargetDrugNDC",
                        column: x => x.TargetDrugNDC,
                        principalTable: "Drugs",
                        principalColumn: "NDC",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugAlternativeReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceDrugNDC = table.Column<string>(type: "text", nullable: false),
                    TargetDrugNDC = table.Column<string>(type: "text", nullable: false),
                    ClassInfoId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StatusDescription = table.Column<string>(type: "text", nullable: false),
                    AdditionalInfo = table.Column<string>(type: "text", nullable: false),
                    StatusDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugAlternativeReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugAlternativeReports_DrugAlternativeStatuses_SourceDrugND~",
                        columns: x => new { x.SourceDrugNDC, x.TargetDrugNDC, x.ClassInfoId },
                        principalTable: "DrugAlternativeStatuses",
                        principalColumns: new[] { "SourceDrugNDC", "TargetDrugNDC", "ClassInfoId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugAlternativeReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlternativeReports_SourceDrugNDC_TargetDrugNDC_ClassInf~",
                table: "DrugAlternativeReports",
                columns: new[] { "SourceDrugNDC", "TargetDrugNDC", "ClassInfoId" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlternativeReports_UserId",
                table: "DrugAlternativeReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlternativeStatuses_ClassInfoId",
                table: "DrugAlternativeStatuses",
                column: "ClassInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlternativeStatuses_TargetDrugNDC",
                table: "DrugAlternativeStatuses",
                column: "TargetDrugNDC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugAlternativeReports");

            migrationBuilder.DropTable(
                name: "DrugAlternativeStatuses");
        }
    }
}
