using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDiseaseVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDiseaseVisibility",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DiseaseId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDiseaseVisibility", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDiseaseVisibility_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDiseaseVisibility_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "DiseaseVisibilitySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 18, 46, 3, 289, DateTimeKind.Utc).AddTicks(7273));

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseVisibility_DiseaseId",
                table: "UserDiseaseVisibility",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseVisibility_UserId_DiseaseId",
                table: "UserDiseaseVisibility",
                columns: new[] { "UserId", "DiseaseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDiseaseVisibility");

            migrationBuilder.UpdateData(
                table: "DiseaseVisibilitySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 17, 16, 35, 488, DateTimeKind.Utc).AddTicks(2117));
        }
    }
}
