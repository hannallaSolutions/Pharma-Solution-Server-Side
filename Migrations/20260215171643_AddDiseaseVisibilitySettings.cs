using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class AddDiseaseVisibilitySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Logs");

            migrationBuilder.CreateTable(
                name: "DiseaseVisibilitySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseVisibilitySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DrugDiseases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DrugId = table.Column<int>(type: "integer", nullable: false),
                    DiseaseId = table.Column<int>(type: "integer", nullable: false),
                    userEmail = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Show = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
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
                        name: "FK_DrugDiseases_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DiseaseVisibilitySettings",
                columns: new[] { "Id", "Mode", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { 1, 1, new DateTime(2026, 2, 15, 17, 16, 35, 488, DateTimeKind.Utc).AddTicks(2117), null });

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_Name",
                table: "Diseases",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseases_DiseaseId",
                table: "DrugDiseases",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseases_DrugId",
                table: "DrugDiseases",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseases_UserId",
                table: "DrugDiseases",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiseaseVisibilitySettings");

            migrationBuilder.DropTable(
                name: "DrugDiseases");

            migrationBuilder.DropIndex(
                name: "IX_Diseases_Name",
                table: "Diseases");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "Logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Logs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
