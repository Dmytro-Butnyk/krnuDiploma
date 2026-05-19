using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveDefenceDateToQualificationWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DefenceDate",
                schema: "diploma",
                table: "QualificationWorks",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE diploma."QualificationWorks" AS qw
                SET "DefenceDate" = d."DefenceDate"
                FROM diploma."Defences" AS d
                WHERE d."QualificationWorkId" = qw."Id";
                """);

            migrationBuilder.DropTable(
                name: "Defences",
                schema: "diploma");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Defences",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiplomaExaminationCommissionId = table.Column<int>(type: "integer", nullable: true),
                    QualificationWorkId = table.Column<int>(type: "integer", nullable: false),
                    DefenceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProtocolNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QueueNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Defences_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                        column: x => x.DiplomaExaminationCommissionId,
                        principalSchema: "diploma",
                        principalTable: "DiplomaExaminationCommissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Defences_QualificationWorks_QualificationWorkId",
                        column: x => x.QualificationWorkId,
                        principalSchema: "diploma",
                        principalTable: "QualificationWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Defences_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Defences",
                column: "DiplomaExaminationCommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Defences_QualificationWorkId",
                schema: "diploma",
                table: "Defences",
                column: "QualificationWorkId",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO diploma."Defences" (
                    "DefenceDate",
                    "QueueNumber",
                    "ProtocolNumber",
                    "QualificationWorkId",
                    "DiplomaExaminationCommissionId")
                SELECT
                    "DefenceDate",
                    0,
                    '',
                    "Id",
                    NULL
                FROM diploma."QualificationWorks"
                WHERE "DefenceDate" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "DefenceDate",
                schema: "diploma",
                table: "QualificationWorks");
        }
    }
}
