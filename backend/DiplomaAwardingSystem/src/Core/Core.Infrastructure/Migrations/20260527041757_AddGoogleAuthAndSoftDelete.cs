using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuthAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "diploma",
                table: "Teachers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                schema: "diploma",
                table: "TeacherPositions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "diploma",
                table: "TeacherPositions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "diploma",
                table: "Specialties",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                schema: "diploma",
                table: "Secretaries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperSecretary",
                schema: "diploma",
                table: "Secretaries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "diploma",
                table: "AcademicDegrees",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secretaries_GoogleSubject",
                schema: "diploma",
                table: "Secretaries",
                column: "GoogleSubject",
                unique: true,
                filter: "\"GoogleSubject\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Secretaries_GoogleSubject",
                schema: "diploma",
                table: "Secretaries");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "diploma",
                table: "TeacherPositions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "diploma",
                table: "Specialties");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                schema: "diploma",
                table: "Secretaries");

            migrationBuilder.DropColumn(
                name: "IsSuperSecretary",
                schema: "diploma",
                table: "Secretaries");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "diploma",
                table: "AcademicDegrees");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                schema: "diploma",
                table: "TeacherPositions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}
