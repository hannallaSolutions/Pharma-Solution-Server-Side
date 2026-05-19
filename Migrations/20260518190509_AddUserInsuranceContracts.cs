using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInsuranceContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserInsuranceContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InsuranceRxId = table.Column<int>(type: "integer", nullable: false),
                    ReimbursementType = table.Column<string>(type: "text", nullable: false),
                    AwpDiscountPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    AspMarkupPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    MacPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    FixedReimbursementAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    DispensingFee = table.Column<decimal>(type: "numeric", nullable: true),
                    ExpectedPatientPay = table.Column<decimal>(type: "numeric", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInsuranceContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInsuranceContracts_InsuranceRxes_InsuranceRxId",
                        column: x => x.InsuranceRxId,
                        principalTable: "InsuranceRxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInsuranceContracts_Users_UserId",
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
                value: new DateTime(2026, 5, 18, 19, 4, 43, 508, DateTimeKind.Utc).AddTicks(3523));

            migrationBuilder.CreateIndex(
                name: "IX_UserInsuranceContracts_InsuranceRxId",
                table: "UserInsuranceContracts",
                column: "InsuranceRxId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInsuranceContracts_UserId",
                table: "UserInsuranceContracts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInsuranceContracts");

            migrationBuilder.UpdateData(
                table: "DiseaseVisibilitySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 5, 17, 18, 54, 20, 238, DateTimeKind.Utc).AddTicks(5731));
        }
    }
}
