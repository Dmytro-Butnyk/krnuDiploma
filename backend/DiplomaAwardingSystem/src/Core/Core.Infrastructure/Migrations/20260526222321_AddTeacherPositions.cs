using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherPositions",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherPositions", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO diploma."TeacherPositions" ("FullName", "ShortName")
                SELECT DISTINCT position_value, position_value
                FROM (
                    SELECT COALESCE(NULLIF(BTRIM("Position"), ''), 'Unknown') AS position_value
                    FROM diploma."Teachers"
                ) AS source
                ORDER BY position_value;
                """);

            migrationBuilder.AddColumn<int>(
                name: "TeacherPositionId",
                schema: "diploma",
                table: "Teachers",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE diploma."Teachers" AS teacher
                SET "TeacherPositionId" = position."Id"
                FROM diploma."TeacherPositions" AS position
                WHERE position."FullName" = COALESCE(NULLIF(BTRIM(teacher."Position"), ''), 'Unknown');
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TeacherPositionId",
                schema: "diploma",
                table: "Teachers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Position",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_TeacherPositionId",
                schema: "diploma",
                table: "Teachers",
                column: "TeacherPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_TeacherPositions_TeacherPositionId",
                schema: "diploma",
                table: "Teachers",
                column: "TeacherPositionId",
                principalSchema: "diploma",
                principalTable: "TeacherPositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_TeacherPositions_TeacherPositionId",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                schema: "diploma",
                table: "Teachers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.Sql(
                """
                UPDATE diploma."Teachers" AS teacher
                SET "Position" = position."FullName"
                FROM diploma."TeacherPositions" AS position
                WHERE position."Id" = teacher."TeacherPositionId";
                """);

            migrationBuilder.DropIndex(
                name: "IX_Teachers_TeacherPositionId",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "TeacherPositionId",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropTable(
                name: "TeacherPositions",
                schema: "diploma");
        }
    }
}
