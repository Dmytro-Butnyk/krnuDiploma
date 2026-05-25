using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommissionHeadsAndCommissionRefactor : Migration
    {
        private static readonly string[] CommissionHeadUniqueColumns =
        [
            "FullName",
            "Position",
            "Company",
            "Specialty"
        ];

        private static readonly string[] CommissionUniqueColumns =
        [
            "DefenseYear",
            "SpecialtyId",
            "EducationLevel"
        ];

        private static readonly string[] CommissionOrderUniqueColumns =
        [
            "DefenseYear",
            "SpecialtyId",
            "OrderNumber"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Specialties_Departments_DepartmentId",
                schema: "diploma",
                table: "Specialties");

            migrationBuilder.CreateTable(
                name: "CommissionHeads",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Position = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Company = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Specialty = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionHeads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionHeads_FullName_Position_Company_Specialty",
                schema: "diploma",
                table: "CommissionHeads",
                columns: CommissionHeadUniqueColumns,
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddColumn<int>(
                name: "CommissionHeadId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefenseYear",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO diploma."CommissionHeads" ("FullName", "Position", "Company", "Specialty", "IsDeleted")
                SELECT DISTINCT
                    COALESCE(NULLIF(BTRIM(dec."HeadPersonaName"), ''), teacher."FullName", 'Unknown') AS "FullName",
                    COALESCE(NULLIF(BTRIM(dec."HeadPersonaPosition"), ''), teacher."Position", 'Unknown') AS "Position",
                    'Unknown' AS "Company",
                    COALESCE(specialty."Name", 'Unknown') AS "Specialty",
                    FALSE AS "IsDeleted"
                FROM diploma."DiplomaExaminationCommissions" dec
                LEFT JOIN diploma."Teachers" teacher ON teacher."Id" = dec."HeadTeacherId"
                LEFT JOIN LATERAL (
                    SELECT g."SpecialtyId", g."Year"
                    FROM diploma."Groups" g
                    WHERE g."DiplomaExaminationCommissionId" = dec."Id"
                    ORDER BY g."Id"
                    LIMIT 1
                ) group_source ON TRUE
                LEFT JOIN diploma."Specialties" specialty ON specialty."Id" = group_source."SpecialtyId";
                """);

            migrationBuilder.Sql(
                """
                WITH source AS (
                    SELECT
                        dec."Id",
                        group_source."Year" AS "DefenseYear",
                        group_source."SpecialtyId",
                        COALESCE(NULLIF(BTRIM(dec."HeadPersonaName"), ''), teacher."FullName", 'Unknown') AS "FullName",
                        COALESCE(NULLIF(BTRIM(dec."HeadPersonaPosition"), ''), teacher."Position", 'Unknown') AS "Position",
                        COALESCE(specialty."Name", 'Unknown') AS "Specialty"
                    FROM diploma."DiplomaExaminationCommissions" dec
                    LEFT JOIN diploma."Teachers" teacher ON teacher."Id" = dec."HeadTeacherId"
                    LEFT JOIN LATERAL (
                        SELECT g."SpecialtyId", g."Year"
                        FROM diploma."Groups" g
                        WHERE g."DiplomaExaminationCommissionId" = dec."Id"
                        ORDER BY g."Id"
                        LIMIT 1
                    ) group_source ON TRUE
                    LEFT JOIN diploma."Specialties" specialty ON specialty."Id" = group_source."SpecialtyId"
                )
                UPDATE diploma."DiplomaExaminationCommissions" dec
                SET
                    "DefenseYear" = source."DefenseYear",
                    "SpecialtyId" = source."SpecialtyId",
                    "CommissionHeadId" = head."Id"
                FROM source
                JOIN diploma."CommissionHeads" head
                    ON head."FullName" = source."FullName"
                    AND head."Position" = source."Position"
                    AND head."Company" = 'Unknown'
                    AND head."Specialty" = source."Specialty"
                WHERE dec."Id" = source."Id";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CommissionHeadId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefenseYear",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Specialties_DepartmentId",
                schema: "diploma",
                table: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "diploma",
                table: "Specialties");

            migrationBuilder.DropColumn(
                name: "HeadPersonaName",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "HeadPersonaPosition",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "diploma");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_CommissionHeadId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "CommissionHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_DefenseYear_SpecialtyId_Educa~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                columns: CommissionUniqueColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_DefenseYear_SpecialtyId_Order~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                columns: CommissionOrderUniqueColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SpecialtyId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_CommissionHeads_CommissionHea~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "CommissionHeadId",
                principalSchema: "diploma",
                principalTable: "CommissionHeads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Specialties_SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SpecialtyId",
                principalSchema: "diploma",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_CommissionHeads_CommissionHea~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Specialties_SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.AddColumn<string>(
                name: "HeadPersonaName",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeadPersonaPosition",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE diploma."DiplomaExaminationCommissions" dec
                SET
                    "HeadPersonaName" = head."FullName",
                    "HeadPersonaPosition" = head."Position"
                FROM diploma."CommissionHeads" head
                WHERE dec."CommissionHeadId" = head."Id";
                """);

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_CommissionHeadId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_DefenseYear_SpecialtyId_Educa~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_DefenseYear_SpecialtyId_Order~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "CommissionHeadId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "DefenseYear",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropTable(
                name: "CommissionHeads",
                schema: "diploma");

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO diploma.\"Departments\" (\"Id\", \"FullName\") VALUES (0, 'Unknown') ON CONFLICT DO NOTHING;");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                schema: "diploma",
                table: "Specialties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_DepartmentId",
                schema: "diploma",
                table: "Specialties",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "HeadTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_HeadTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "HeadTeacherId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Specialties_Departments_DepartmentId",
                schema: "diploma",
                table: "Specialties",
                column: "DepartmentId",
                principalSchema: "diploma",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
