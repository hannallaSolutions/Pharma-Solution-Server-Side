using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SearchTool_ServerSide.Migrations
{
    public partial class AddUserBranchesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                  //  AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
              AssignedAt = table.Column<DateTime>(
    type: "timestamp with time zone",
    nullable: false,
    defaultValueSql: "NOW()")
               },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBranches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBranches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBranches_UserId",
                table: "UserBranches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBranches_BranchId",
                table: "UserBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBranches_UserId_BranchId",
                table: "UserBranches",
                columns: new[] { "UserId", "BranchId" },
                unique: true);

            // Idempotent backfill: seeds one UserBranches row per existing user.
            // NOT EXISTS guard makes this safe to re-run without creating duplicates.
            migrationBuilder.Sql(@"
                INSERT INTO ""UserBranches"" (""UserId"", ""BranchId"", ""IsDefault"", ""IsActive"", ""AssignedAt"")
                SELECT u.""Id"", u.""BranchId"", true, true, NOW()
                FROM ""Users"" u
                INNER JOIN ""Branches"" b ON b.""Id"" = u.""BranchId""
                WHERE u.""BranchId"" IS NOT NULL
                  AND u.""BranchId"" != 0
                  AND NOT EXISTS (
                      SELECT 1 FROM ""UserBranches"" ub
                      WHERE ub.""UserId"" = u.""Id"" AND ub.""BranchId"" = u.""BranchId""
                  );
            ");

            // Validation query — run manually after migration to find users skipped by the backfill.
            // These users have a NULL, zero, or non-existent BranchId and need admin attention.
            //
            // SELECT u."Id", u."Email", u."BranchId",
            //        CASE
            //            WHEN u."BranchId" IS NULL THEN 'BranchId is NULL'
            //            WHEN u."BranchId" = 0     THEN 'BranchId is 0'
            //            ELSE                           'BranchId does not exist in Branches'
            //        END AS "Issue"
            // FROM "Users" u
            // LEFT JOIN "Branches" b ON b."Id"  = u."BranchId"
            // WHERE u."BranchId" IS NULL
            //    OR u."BranchId" = 0
            //    OR b."Id" IS NULL;
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBranches");
        }
    }
}
