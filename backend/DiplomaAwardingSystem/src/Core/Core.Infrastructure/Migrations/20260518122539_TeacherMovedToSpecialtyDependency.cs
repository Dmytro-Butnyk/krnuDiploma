using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeacherMovedToSpecialtyDependency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Departments_DepartmentId",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                schema: "diploma",
                table: "Teachers",
                newName: "SpecialtyId");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_DepartmentId",
                schema: "diploma",
                table: "Teachers",
                newName: "IX_Teachers_SpecialtyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Specialties_SpecialtyId",
                schema: "diploma",
                table: "Teachers",
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
                name: "FK_Teachers_Specialties_SpecialtyId",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.RenameColumn(
                name: "SpecialtyId",
                schema: "diploma",
                table: "Teachers",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_SpecialtyId",
                schema: "diploma",
                table: "Teachers",
                newName: "IX_Teachers_DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Departments_DepartmentId",
                schema: "diploma",
                table: "Teachers",
                column: "DepartmentId",
                principalSchema: "diploma",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
