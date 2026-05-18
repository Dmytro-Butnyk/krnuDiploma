using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredDeleteBehaviorForStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Defences_QualificationWorks_QualificationWorkId",
                schema: "diploma",
                table: "Defences");

            migrationBuilder.DropForeignKey(
                name: "FK_QualificationWorks_Students_StudentId",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.AddForeignKey(
                name: "FK_Defences_QualificationWorks_QualificationWorkId",
                schema: "diploma",
                table: "Defences",
                column: "QualificationWorkId",
                principalSchema: "diploma",
                principalTable: "QualificationWorks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationWorks_Students_StudentId",
                schema: "diploma",
                table: "QualificationWorks",
                column: "StudentId",
                principalSchema: "diploma",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Defences_QualificationWorks_QualificationWorkId",
                schema: "diploma",
                table: "Defences");

            migrationBuilder.DropForeignKey(
                name: "FK_QualificationWorks_Students_StudentId",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.AddForeignKey(
                name: "FK_Defences_QualificationWorks_QualificationWorkId",
                schema: "diploma",
                table: "Defences",
                column: "QualificationWorkId",
                principalSchema: "diploma",
                principalTable: "QualificationWorks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationWorks_Students_StudentId",
                schema: "diploma",
                table: "QualificationWorks",
                column: "StudentId",
                principalSchema: "diploma",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
