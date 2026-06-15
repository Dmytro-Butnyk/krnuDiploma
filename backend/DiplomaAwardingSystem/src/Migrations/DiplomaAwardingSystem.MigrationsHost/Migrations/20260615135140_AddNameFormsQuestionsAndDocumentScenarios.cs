using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    public partial class AddNameFormsQuestionsAndDocumentScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teachers_SpecialtyId",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Students_GroupId",
                schema: "diploma",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Groups_SpecialtyId",
                schema: "diploma",
                table: "Groups");

            migrationBuilder.AddColumn<string>(
                name: "NameDative",
                schema: "diploma",
                table: "Teachers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameGenitive",
                schema: "diploma",
                table: "Teachers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameNominative",
                schema: "diploma",
                table: "Teachers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameSignature",
                schema: "diploma",
                table: "Teachers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "GenitiveFullName",
                schema: "diploma",
                table: "TeacherPositions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "GenitiveShortName",
                schema: "diploma",
                table: "TeacherPositions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameDative",
                schema: "diploma",
                table: "Students",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameGenitive",
                schema: "diploma",
                table: "Students",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameNominative",
                schema: "diploma",
                table: "Students",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameSignature",
                schema: "diploma",
                table: "Students",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "DefenceQuestions",
                schema: "diploma",
                table: "QualificationWorks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameDative",
                schema: "diploma",
                table: "CommissionHeads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameGenitive",
                schema: "diploma",
                table: "CommissionHeads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameNominative",
                schema: "diploma",
                table: "CommissionHeads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NameSignature",
                schema: "diploma",
                table: "CommissionHeads",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "GenitiveFullName",
                schema: "diploma",
                table: "AcademicDegrees",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "GenitiveShortName",
                schema: "diploma",
                table: "AcademicDegrees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateTable(
                name: "DocumentConstructorScenarios",
                schema: "diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ScenarioJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentConstructorScenarios", x => x.Id);
                });

            migrationBuilder.Sql("""
                UPDATE diploma."Students"
                SET "NameNominative" = "FullName",
                    "NameGenitive" = "FullName",
                    "NameDative" = "FullName",
                    "NameSignature" = "FullName"
                WHERE "NameNominative" = '';

                UPDATE diploma."Teachers"
                SET "NameNominative" = "FullName",
                    "NameGenitive" = "FullName",
                    "NameDative" = "FullName",
                    "NameSignature" = "ShortName"
                WHERE "NameNominative" = '';

                UPDATE diploma."CommissionHeads"
                SET "NameNominative" = "FullName",
                    "NameGenitive" = "FullName",
                    "NameDative" = "FullName",
                    "NameSignature" = "FullName"
                WHERE "NameNominative" = '';

                UPDATE diploma."AcademicDegrees"
                SET "GenitiveFullName" = "FullName",
                    "GenitiveShortName" = "ShortName"
                WHERE "GenitiveFullName" = '';

                UPDATE diploma."TeacherPositions"
                SET "GenitiveFullName" = "FullName",
                    "GenitiveShortName" = "ShortName"
                WHERE "GenitiveFullName" = '';

                UPDATE diploma."QualificationWorks"
                SET "DefenceQuestions" = '[]'::jsonb
                WHERE "DefenceQuestions" IS NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO diploma."DocumentConstructorScenarios" ("Code", "Title", "Description", "ScenarioJson", "IsActive")
                VALUES
                (
                    'group-defence-day-extract',
                    'Group defence day extract',
                    'Group, defence date, and students defended on selected date with score 60+.',
                    $scenario$
                    {
                      "Id": "group-defence-day-extract",
                      "Title": "Group defence day extract",
                      "Description": "Group, defence date, and students defended on selected date with score 60+.",
                      "Inputs": {
                        "GroupId": {
                          "Kind": "EntitySelect",
                          "ValueType": "Int",
                          "Label": "Group",
                          "Required": true,
                          "Entity": "Group",
                          "ValuePath": null,
                          "DependsOn": [],
                          "Filters": [],
                          "Display": ["Name", "Year"],
                          "Description": [],
                          "Search": ["Name", "Year"],
                          "OrderBy": ["Year desc", "Name"],
                          "MaxLength": null
                        },
                        "DefenceDate": {
                          "Kind": "ValueSelect",
                          "ValueType": "Date",
                          "Label": "Defence date",
                          "Required": true,
                          "Entity": "QualificationWork",
                          "ValuePath": "DefenceDate",
                          "DependsOn": ["GroupId"],
                          "Filters": [
                            { "Property": "Student.GroupId", "Operator": "Equals", "Input": "GroupId" }
                          ],
                          "Display": [],
                          "Description": [],
                          "Search": [],
                          "OrderBy": ["DefenceDate"],
                          "MaxLength": null
                        }
                      },
                      "DataSources": [
                        {
                          "Key": "TargetGroup",
                          "Entity": "Group",
                          "Result": "One",
                          "Filter": "Id == @0",
                          "FilterArgs": ["GroupId"],
                          "Includes": ["Specialty", "DiplomaExaminationCommission.Secretary"],
                          "OrderBy": []
                        },
                        {
                          "Key": "DayStudents",
                          "Entity": "Student",
                          "Result": "Many",
                          "Filter": "GroupId == @0 && QualificationWork.DefenceDate == @1 && QualificationWork.CommissionScore >= 60",
                          "FilterArgs": ["GroupId", "DefenceDate"],
                          "Includes": ["Group.Specialty", "QualificationWork"],
                          "OrderBy": ["FullName"]
                        }
                      ],
                      "RecommendedTableSources": [
                        { "Key": "DayStudents", "Label": "Students by defence date", "Entity": "Student" }
                      ],
                      "RequiredScalarMappings": [
                        {
                          "Tag": "DefenceDate",
                          "Path": "Input.DefenceDate",
                          "Message": "Map tag DefenceDate to Input.DefenceDate."
                        },
                        {
                          "Tag": "ProtocolsNumbers",
                          "Path": "Computed.ProtocolsNumbers",
                          "Message": "Map tag ProtocolsNumbers to Computed.ProtocolsNumbers."
                        }
                      ],
                      "RequiredTableSources": [
                        {
                          "SourceArray": "DayStudents",
                          "Message": "Use DayStudents as the table source to keep the defence-date filter."
                        }
                      ],
                      "HelperKeys": ["ProtocolsNumbers"]
                    }
                    $scenario$::jsonb,
                    true
                ),
                (
                    'single-qualification-work-protocol',
                    'Single qualification work protocol',
                    'Protocol for one student qualification work defence with prebuilt protocol helper fields.',
                    $scenario$
                    {
                      "Id": "single-qualification-work-protocol",
                      "Title": "Single qualification work protocol",
                      "Description": "Protocol for one student qualification work defence with prebuilt protocol helper fields.",
                      "Inputs": {
                        "StudentId": {
                          "Kind": "EntitySelect",
                          "ValueType": "Int",
                          "Label": "Student",
                          "Required": true,
                          "Entity": "Student",
                          "ValuePath": null,
                          "DependsOn": [],
                          "Filters": [],
                          "Display": ["FullName"],
                          "Description": [],
                          "Search": ["FullName"],
                          "OrderBy": ["FullName"],
                          "MaxLength": null
                        },
                        "ProtocolNumber": {
                          "Kind": "Manual",
                          "ValueType": "String",
                          "Label": "Protocol number",
                          "Required": true,
                          "Entity": null,
                          "ValuePath": null,
                          "DependsOn": [],
                          "Filters": [],
                          "Display": [],
                          "Description": [],
                          "Search": [],
                          "OrderBy": [],
                          "MaxLength": 50
                        },
                        "MeetingDate": {
                          "Kind": "Manual",
                          "ValueType": "Date",
                          "Label": "Meeting date",
                          "Required": true,
                          "Entity": null,
                          "ValuePath": null,
                          "DependsOn": [],
                          "Filters": [],
                          "Display": [],
                          "Description": [],
                          "Search": [],
                          "OrderBy": [],
                          "MaxLength": null
                        },
                        "MeetingStartTime": {
                          "Kind": "Manual",
                          "ValueType": "String",
                          "Label": "Meeting start time",
                          "Required": false,
                          "Entity": null,
                          "ValuePath": null,
                          "DependsOn": [],
                          "Filters": [],
                          "Display": [],
                          "Description": [],
                          "Search": [],
                          "OrderBy": [],
                          "MaxLength": 20
                        },
                        "MeetingEndTime": {
                          "Kind": "Manual",
                          "ValueType": "String",
                          "Label": "Meeting end time",
                          "Required": false,
                          "Entity": null,
                          "ValuePath": null,
                          "DependsOn": [],
                          "Filters": [],
                          "Display": [],
                          "Description": [],
                          "Search": [],
                          "OrderBy": [],
                          "MaxLength": 20
                        }
                      },
                      "DataSources": [
                        {
                          "Key": "TargetStudent",
                          "Entity": "Student",
                          "Result": "One",
                          "Filter": "Id == @0",
                          "FilterArgs": ["StudentId"],
                          "Includes": [
                            "Group.Specialty",
                            "Group.DiplomaExaminationCommission.CommissionHead",
                            "Group.DiplomaExaminationCommission.Secretary",
                            "Group.DiplomaExaminationCommission.FirstMemberTeacher.AcademicDegree",
                            "Group.DiplomaExaminationCommission.FirstMemberTeacher.TeacherPosition",
                            "Group.DiplomaExaminationCommission.SecondMemberTeacher.AcademicDegree",
                            "Group.DiplomaExaminationCommission.SecondMemberTeacher.TeacherPosition",
                            "Group.DiplomaExaminationCommission.ThirdMemberTeacher.AcademicDegree",
                            "Group.DiplomaExaminationCommission.ThirdMemberTeacher.TeacherPosition",
                            "QualificationWork.Teacher.AcademicDegree",
                            "QualificationWork.Teacher.TeacherPosition",
                            "QualificationWork.Reviewer.AcademicDegree",
                            "QualificationWork.Reviewer.TeacherPosition",
                            "QualificationWork.QualificationWorkCharacteristics"
                          ],
                          "OrderBy": []
                        }
                      ],
                      "RecommendedTableSources": [
                        { "Key": "Computed.DefenceQuestions", "Label": "Defence questions", "Entity": "Computed" }
                      ],
                      "RequiredScalarMappings": [
                        { "Tag": "StudentNameGenitive", "Path": "Computed.StudentNameGenitive", "Message": "Use computed student genitive name." },
                        { "Tag": "StudentNameDative", "Path": "Computed.StudentNameDative", "Message": "Use computed student dative name." },
                        { "Tag": "SupervisorLine", "Path": "Computed.SupervisorLine", "Message": "Use computed supervisor line." },
                        { "Tag": "ReviewerLine", "Path": "Computed.ReviewerLine", "Message": "Use computed reviewer line." }
                      ],
                      "RequiredTableSources": [
                        { "SourceArray": "Computed.DefenceQuestions", "Message": "Use computed defence questions table source." }
                      ],
                      "HelperKeys": ["SingleQualificationWorkProtocol"]
                    }
                    $scenario$::jsonb,
                    true
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SpecialtyId_IsActive_ShortName",
                schema: "diploma",
                table: "Teachers",
                columns: new[] { "SpecialtyId", "IsActive", "ShortName" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_GroupId_FullName",
                schema: "diploma",
                table: "Students",
                columns: new[] { "GroupId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SpecialtyId_EducationLevel_Year_Name",
                schema: "diploma",
                table: "Groups",
                columns: new[] { "SpecialtyId", "EducationLevel", "Year", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentConstructorScenarios_Code",
                schema: "diploma",
                table: "DocumentConstructorScenarios",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentConstructorScenarios",
                schema: "diploma");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_SpecialtyId_IsActive_ShortName",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Students_GroupId_FullName",
                schema: "diploma",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Groups_SpecialtyId_EducationLevel_Year_Name",
                schema: "diploma",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "NameDative",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "NameGenitive",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "NameNominative",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "NameSignature",
                schema: "diploma",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "GenitiveFullName",
                schema: "diploma",
                table: "TeacherPositions");

            migrationBuilder.DropColumn(
                name: "GenitiveShortName",
                schema: "diploma",
                table: "TeacherPositions");

            migrationBuilder.DropColumn(
                name: "NameDative",
                schema: "diploma",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "NameGenitive",
                schema: "diploma",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "NameNominative",
                schema: "diploma",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "NameSignature",
                schema: "diploma",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DefenceQuestions",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "NameDative",
                schema: "diploma",
                table: "CommissionHeads");

            migrationBuilder.DropColumn(
                name: "NameGenitive",
                schema: "diploma",
                table: "CommissionHeads");

            migrationBuilder.DropColumn(
                name: "NameNominative",
                schema: "diploma",
                table: "CommissionHeads");

            migrationBuilder.DropColumn(
                name: "NameSignature",
                schema: "diploma",
                table: "CommissionHeads");

            migrationBuilder.DropColumn(
                name: "GenitiveFullName",
                schema: "diploma",
                table: "AcademicDegrees");

            migrationBuilder.DropColumn(
                name: "GenitiveShortName",
                schema: "diploma",
                table: "AcademicDegrees");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SpecialtyId",
                schema: "diploma",
                table: "Teachers",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_GroupId",
                schema: "diploma",
                table: "Students",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SpecialtyId",
                schema: "diploma",
                table: "Groups",
                column: "SpecialtyId");
        }
    }
}
