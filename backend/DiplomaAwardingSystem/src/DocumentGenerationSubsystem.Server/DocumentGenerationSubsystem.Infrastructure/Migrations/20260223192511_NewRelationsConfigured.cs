using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentGenerationSubsystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewRelationsConfigured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_qualification_works_students_StudentId",
                schema: "diploma",
                table: "qualification_works");

            migrationBuilder.AddForeignKey(
                name: "FK_qualification_works_students_StudentId",
                schema: "diploma",
                table: "qualification_works",
                column: "StudentId",
                principalSchema: "diploma",
                principalTable: "students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_qualification_works_students_StudentId",
                schema: "diploma",
                table: "qualification_works");

            migrationBuilder.AddForeignKey(
                name: "FK_qualification_works_students_StudentId",
                schema: "diploma",
                table: "qualification_works",
                column: "StudentId",
                principalSchema: "diploma",
                principalTable: "students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
