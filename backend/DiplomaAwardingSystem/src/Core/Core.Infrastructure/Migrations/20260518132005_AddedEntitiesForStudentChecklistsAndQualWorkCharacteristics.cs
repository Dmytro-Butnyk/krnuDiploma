using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedEntitiesForStudentChecklistsAndQualWorkCharacteristics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectronicComponentsChecklists",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HasRegulatoryControl = table.Column<bool>(type: "boolean", nullable: false),
                    HasExplanatoryNoteDoc = table.Column<bool>(type: "boolean", nullable: false),
                    HasExplanatoryNotePdf = table.Column<bool>(type: "boolean", nullable: false),
                    HasPlagiarismReportPdf = table.Column<bool>(type: "boolean", nullable: false),
                    HasReviewDoc = table.Column<bool>(type: "boolean", nullable: false),
                    HasPresentationPpt = table.Column<bool>(type: "boolean", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicComponentsChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectronicComponentsChecklists_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "diploma",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalComponentsChecklists",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HasStudentCard = table.Column<bool>(type: "boolean", nullable: false),
                    HasGradeBook = table.Column<bool>(type: "boolean", nullable: false),
                    HasCircular = table.Column<bool>(type: "boolean", nullable: false),
                    HasSignedReview = table.Column<bool>(type: "boolean", nullable: false),
                    HasCopyOfBankReceipt = table.Column<bool>(type: "boolean", nullable: false),
                    HasExplanatoryNote = table.Column<bool>(type: "boolean", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalComponentsChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalComponentsChecklists_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "diploma",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualificationWorkCharacteristics",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsResearchBased = table.Column<bool>(type: "boolean", nullable: false),
                    HasRealProjects = table.Column<bool>(type: "boolean", nullable: false),
                    IsEcoFriendly = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnterpriseOrdered = table.Column<bool>(type: "boolean", nullable: false),
                    IsComplexInteruniversity = table.Column<bool>(type: "boolean", nullable: false),
                    IsComplexInterdepartmental = table.Column<bool>(type: "boolean", nullable: false),
                    IsComplexDepartmental = table.Column<bool>(type: "boolean", nullable: false),
                    IsComplexProjectParticipant = table.Column<bool>(type: "boolean", nullable: false),
                    IsRecommendedForMaster = table.Column<bool>(type: "boolean", nullable: false),
                    IsRecommendedForImplementation = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefendedAtEnterprise = table.Column<bool>(type: "boolean", nullable: false),
                    QualificationWorkId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationWorkCharacteristics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualificationWorkCharacteristics_QualificationWorks_Qualifi~",
                        column: x => x.QualificationWorkId,
                        principalSchema: "diploma",
                        principalTable: "QualificationWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicComponentsChecklists_StudentId",
                schema: "diploma",
                table: "ElectronicComponentsChecklists",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalComponentsChecklists_StudentId",
                schema: "diploma",
                table: "PhysicalComponentsChecklists",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualificationWorkCharacteristics_QualificationWorkId",
                schema: "diploma",
                table: "QualificationWorkCharacteristics",
                column: "QualificationWorkId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectronicComponentsChecklists",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "PhysicalComponentsChecklists",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "QualificationWorkCharacteristics",
                schema: "diploma");
        }
    }
}
