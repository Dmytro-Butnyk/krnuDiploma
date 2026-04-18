using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#pragma warning disable
#nullable disable

namespace DocumentGenerationSubsystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    ShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicDegrees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
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
                    DepartmentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specialties_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "diploma",
                        principalTable: "Departments",
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
                    ShortName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Position = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AcademicDegreeId = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false)
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
                        name: "FK_Teachers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "diploma",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    SpecialtyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalSchema: "diploma",
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DecMembers",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Role = table.Column<string>(type: "text", nullable: false),
                    TeacherId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecMembers_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalSchema: "diploma",
                        principalTable: "Teachers",
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
                    OrderNumber = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiplomaExaminationCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiplomaExaminationCommissions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "diploma",
                        principalTable: "Groups",
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
                    DiplomaExaminationCommissionId = table.Column<int>(type: "integer", nullable: false)
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DecToMembers",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DecMemberId = table.Column<int>(type: "integer", nullable: false),
                    DiplomaExaminationCommissionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecToMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecToMembers_DecMembers_DecMemberId",
                        column: x => x.DecMemberId,
                        principalSchema: "diploma",
                        principalTable: "DecMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecToMembers_DiplomaExaminationCommissions_DiplomaExaminati~",
                        column: x => x.DiplomaExaminationCommissionId,
                        principalSchema: "diploma",
                        principalTable: "DiplomaExaminationCommissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    TeacherId = table.Column<int>(type: "integer", nullable: false)
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
                name: "Defences",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QueueNumber = table.Column<int>(type: "integer", nullable: false),
                    ProtocolNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QualificationWorkId = table.Column<int>(type: "integer", nullable: false),
                    DiplomaExaminationCommissionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Defences_DiplomaExaminationCommissions_DiplomaExaminationCo~",
                        column: x => x.DiplomaExaminationCommissionId,
                        principalSchema: "diploma",
                        principalTable: "DiplomaExaminationCommissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Defences_QualificationWorks_QualificationWorkId",
                        column: x => x.QualificationWorkId,
                        principalSchema: "diploma",
                        principalTable: "QualificationWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Archives_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Archives",
                column: "DiplomaExaminationCommissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecMembers_TeacherId",
                schema: "diploma",
                table: "DecMembers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DecToMembers_DecMemberId_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "DecToMembers",
                columns: new[] { "DecMemberId", "DiplomaExaminationCommissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecToMembers_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "DecToMembers",
                column: "DiplomaExaminationCommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Defences_DiplomaExaminationCommissionId",
                schema: "diploma",
                table: "Defences",
                column: "DiplomaExaminationCommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Defences_QualificationWorkId",
                schema: "diploma",
                table: "Defences",
                column: "QualificationWorkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_GroupId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "GroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SpecialtyId",
                schema: "diploma",
                table: "Groups",
                column: "SpecialtyId");

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
                name: "IX_Specialties_Code",
                schema: "diploma",
                table: "Specialties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_DepartmentId",
                schema: "diploma",
                table: "Specialties",
                column: "DepartmentId");

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
                name: "IX_Teachers_DepartmentId",
                schema: "diploma",
                table: "Teachers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_Email",
                schema: "diploma",
                table: "Teachers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Archives",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "DecToMembers",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Defences",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "DocumentTemplates",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "DecMembers",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "DiplomaExaminationCommissions",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "QualificationWorks",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Teachers",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "AcademicDegrees",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Specialties",
                schema: "diploma");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "diploma");
        }
    }
}
