using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StudentDetailsPageSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasDiplomaWithHonors",
                schema: "diploma",
                table: "QualificationWorks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PracticeBase",
                schema: "diploma",
                table: "QualificationWorks",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "ReviewerId",
                schema: "diploma",
                table: "QualificationWorks",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Defences",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DefenceDate",
                schema: "diploma",
                table: "Defences",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationWorks_ReviewerId",
                schema: "diploma",
                table: "QualificationWorks",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationWorks_Teachers_ReviewerId",
                schema: "diploma",
                table: "QualificationWorks",
                column: "ReviewerId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QualificationWorks_Teachers_ReviewerId",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropIndex(
                name: "IX_QualificationWorks_ReviewerId",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "HasDiplomaWithHonors",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "PracticeBase",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.AlterColumn<int>(
                name: "DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Defences",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DefenceDate",
                schema: "diploma",
                table: "Defences",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
