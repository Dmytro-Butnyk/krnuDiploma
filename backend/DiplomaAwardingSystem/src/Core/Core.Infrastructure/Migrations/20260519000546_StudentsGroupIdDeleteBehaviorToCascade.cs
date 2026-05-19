using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StudentsGroupIdDeleteBehaviorToCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Groups_GroupId",
                schema: "diploma",
                table: "Students");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Groups_GroupId",
                schema: "diploma",
                table: "Students",
                column: "GroupId",
                principalSchema: "diploma",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Groups_GroupId",
                schema: "diploma",
                table: "Students");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Groups_GroupId",
                schema: "diploma",
                table: "Students",
                column: "GroupId",
                principalSchema: "diploma",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
