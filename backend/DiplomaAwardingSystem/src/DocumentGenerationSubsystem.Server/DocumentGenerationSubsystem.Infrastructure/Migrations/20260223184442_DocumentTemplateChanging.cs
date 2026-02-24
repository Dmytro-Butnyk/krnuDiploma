using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable
#nullable disable

namespace DocumentGenerationSubsystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentTemplateChanging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Удаляем колонку, которая определена как smallint[]
            migrationBuilder.DropColumn(
                name: "WordTemplate",
                schema: "diploma",
                table: "document_templates");

            // 2. Добавляем её заново с правильным типом bytea
            migrationBuilder.AddColumn<byte[]>(
                name: "WordTemplate",
                schema: "diploma",
                table: "document_templates",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Обратная операция для отката
            migrationBuilder.DropColumn(
                name: "WordTemplate",
                schema: "diploma",
                table: "document_templates");

            migrationBuilder.AddColumn<byte[]>(
                name: "WordTemplate",
                schema: "diploma",
                table: "document_templates",
                type: "smallint[]",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
