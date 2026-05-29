using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "diploma");

            migrationBuilder.CreateTable(
                name: "AcademicDegrees",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicDegrees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionHeads",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Position = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Company = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Specialty = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionHeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTemplates",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WordTemplate = table.Column<byte[]>(type: "bytea", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherPositions",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherPositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Secretaries",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    GoogleSubject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuperSecretary = table.Column<bool>(type: "boolean", nullable: false),
                    SpecialtyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secretaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Secretaries_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalSchema: "diploma",
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AcademicDegreeId = table.Column<int>(type: "integer", nullable: false),
                    TeacherPositionId = table.Column<int>(type: "integer", nullable: false),
                    SpecialtyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_AcademicDegrees_AcademicDegreeId",
                        column: x => x.AcademicDegreeId,
                        principalSchema: "diploma",
                        principalTable: "AcademicDegrees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teachers_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalSchema: "diploma",
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teachers_TeacherPositions_TeacherPositionId",
                        column: x => x.TeacherPositionId,
                        principalSchema: "diploma",
                        principalTable: "TeacherPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiplomaExaminationCommissions",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EducationLevel = table.Column<string>(type: "text", nullable: false),
                    DefenseYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SpecialtyId = table.Column<int>(type: "integer", nullable: false),
                    CommissionHeadId = table.Column<int>(type: "integer", nullable: false),
                    FirstMemberTeacherId = table.Column<int>(type: "integer", nullable: false),
                    SecondMemberTeacherId = table.Column<int>(type: "integer", nullable: false),
                    ThirdMemberTeacherId = table.Column<int>(type: "integer", nullable: false),
                    SecretaryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiplomaExaminationCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiplomaExaminationCommissions_CommissionHeads_CommissionHea~",
                        column: x => x.CommissionHeadId,
                        principalSchema: "diploma",
                        principalTable: "CommissionHeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiplomaExaminationCommissions_Secretaries_SecretaryId",
                        column: x => x.SecretaryId,
                        principalSchema: "diploma",
                        principalTable: "Secretaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiplomaExaminationCommissions_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalSchema: "diploma",
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiplomaExaminationCommissions_Teachers_FirstMemberTeacherId",
                        column: x => x.FirstMemberTeacherId,
                        principalSchema: "diploma",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiplomaExaminationCommissions_Teachers_SecondMemberTeacherId",
                        column: x => x.SecondMemberTeacherId,
                        principalSchema: "diploma",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiplomaExaminationCommissions_Teachers_ThirdMemberTeacherId",
                        column: x => x.ThirdMemberTeacherId,
                        principalSchema: "diploma",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Archives",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProtocolRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalPages = table.Column<int>(type: "integer", nullable: false),
                    DiplomaExaminationCommissionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Archives_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                        column: x => x.DiplomaExaminationCommissionId,
                        principalSchema: "diploma",
                        principalTable: "DiplomaExaminationCommissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EducationLevel = table.Column<string>(type: "text", nullable: false),
                    SpecialtyId = table.Column<int>(type: "integer", nullable: false),
                    DiplomaExaminationCommissionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_DiplomaExaminationCommissions_DiplomaExaminationComm~",
                        column: x => x.DiplomaExaminationCommissionId,
                        principalSchema: "diploma",
                        principalTable: "DiplomaExaminationCommissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Groups_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalSchema: "diploma",
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "diploma",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "QualificationWorks",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Topic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PagesCount = table.Column<int>(type: "integer", nullable: false),
                    PlagiarismPercent = table.Column<float>(type: "real", nullable: false),
                    UniquePercent = table.Column<float>(type: "real", nullable: false),
                    SupervisorScore = table.Column<int>(type: "integer", nullable: false),
                    ReviewerScore = table.Column<int>(type: "integer", nullable: false),
                    CommissionScore = table.Column<int>(type: "integer", nullable: false),
                    EctsGrade = table.Column<string>(type: "text", nullable: false),
                    NationalGrade = table.Column<string>(type: "text", nullable: false),
                    PracticeBase = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HasDiplomaWithHonors = table.Column<bool>(type: "boolean", nullable: false),
                    DefenceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    TeacherId = table.Column<int>(type: "integer", nullable: true),
                    ReviewerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationWorks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualificationWorks_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "diploma",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualificationWorks_Teachers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalSchema: "diploma",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualificationWorks_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalSchema: "diploma",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_Archives_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Archives",
                column: "DiplomaExaminationCommissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionHeads_FullName_Position_Company_Specialty",
                schema: "diploma",
                table: "CommissionHeads",
                columns: new[] { "FullName", "Position", "Company", "Specialty" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_CommissionHeadId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "CommissionHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_DefenseYear_SpecialtyId_Educa~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                columns: new[] { "DefenseYear", "SpecialtyId", "EducationLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_DefenseYear_SpecialtyId_Order~",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                columns: new[] { "DefenseYear", "SpecialtyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_FirstMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "FirstMemberTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_SecondMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecondMemberTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_SecretaryId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecretaryId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_SpecialtyId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_ThirdMemberTeacherId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "ThirdMemberTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicComponentsChecklists_StudentId",
                schema: "diploma",
                table: "ElectronicComponentsChecklists",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Groups",
                column: "DiplomaExaminationCommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SpecialtyId",
                schema: "diploma",
                table: "Groups",
                column: "SpecialtyId");

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

            migrationBuilder.CreateIndex(
                name: "IX_QualificationWorks_ReviewerId",
                schema: "diploma",
                table: "QualificationWorks",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationWorks_StudentId",
                schema: "diploma",
                table: "QualificationWorks",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualificationWorks_TeacherId",
                schema: "diploma",
                table: "QualificationWorks",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Secretaries_Email",
                schema: "diploma",
                table: "Secretaries",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secretaries_GoogleSubject",
                schema: "diploma",
                table: "Secretaries",
                column: "GoogleSubject",
                unique: true,
                filter: "\"GoogleSubject\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secretaries_SpecialtyId",
                schema: "diploma",
                table: "Secretaries",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_Code",
                schema: "diploma",
                table: "Specialties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_GroupId",
                schema: "diploma",
                table: "Students",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_AcademicDegreeId",
                schema: "diploma",
                table: "Teachers",
                column: "AcademicDegreeId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_Email",
                schema: "diploma",
                table: "Teachers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SpecialtyId",
                schema: "diploma",
                table: "Teachers",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_TeacherPositionId",
                schema: "diploma",
                table: "Teachers",
                column: "TeacherPositionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Archives",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "DocumentTemplates",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "ElectronicComponentsChecklists",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "PhysicalComponentsChecklists",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "QualificationWorkCharacteristics",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "QualificationWorks",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "DiplomaExaminationCommissions",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "CommissionHeads",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Secretaries",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Teachers",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "AcademicDegrees",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Specialties",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "TeacherPositions",
                schema: "diploma");
        }
    }
}
