using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class AddMainCompanyFeatureSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "MainCompanyFeatureSettings",
        columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            MainCompanyId = table.Column<int>(type: "integer", nullable: false),
            FeatureKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            SelectedOptionKeysJson = table.Column<string>(type: "text", nullable: false),
            IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_MainCompanyFeatureSettings", x => x.Id);
        });

    migrationBuilder.CreateIndex(
        name: "IX_MainCompanyFeatureSettings_MainCompanyId_FeatureKey",
        table: "MainCompanyFeatureSettings",
        columns: new[] { "MainCompanyId", "FeatureKey" },
        unique: true);
}
        /// <inheritdoc />
       protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(
        name: "MainCompanyFeatureSettings");
}
    }
}
