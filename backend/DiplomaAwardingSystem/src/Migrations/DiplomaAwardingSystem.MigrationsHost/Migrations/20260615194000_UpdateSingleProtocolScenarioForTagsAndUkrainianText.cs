using Core.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DbDocGenContext))]
    [Migration("20260615194000_UpdateSingleProtocolScenarioForTagsAndUkrainianText")]
    public partial class UpdateSingleProtocolScenarioForTagsAndUkrainianText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "Title" = 'Протокол захисту кваліфікаційної роботи',
                    "Description" = 'Сценарій для формування протоколу захисту кваліфікаційної роботи одного студента.',
                    "ScenarioJson" = $scenario$
                    {
                      "Id": "single-qualification-work-protocol",
                      "Title": "Протокол захисту кваліфікаційної роботи",
                      "Inputs": {
                        "StudentId": {
                          "Kind": "EntitySelect",
                          "Label": "Студент",
                          "Entity": "Student",
                          "Search": [
                            "FullName"
                          ],
                          "Display": [
                            "FullName"
                          ],
                          "Filters": [],
                          "OrderBy": [
                            "FullName"
                          ],
                          "Required": true,
                          "DependsOn": [],
                          "MaxLength": null,
                          "ValuePath": null,
                          "ValueType": "Int",
                          "Description": []
                        },
                        "MeetingDate": {
                          "Kind": "Manual",
                          "Label": "Дата засідання",
                          "Entity": null,
                          "Search": [],
                          "Display": [],
                          "Filters": [],
                          "OrderBy": [],
                          "Required": true,
                          "DependsOn": [],
                          "MaxLength": null,
                          "ValuePath": null,
                          "ValueType": "Date",
                          "Description": []
                        },
                        "ProtocolNumber": {
                          "Kind": "Manual",
                          "Label": "Номер протоколу",
                          "Entity": null,
                          "Search": [],
                          "Display": [],
                          "Filters": [],
                          "OrderBy": [],
                          "Required": true,
                          "DependsOn": [],
                          "MaxLength": 50,
                          "ValuePath": null,
                          "ValueType": "String",
                          "Description": []
                        },
                        "MeetingStartHour": {
                          "Kind": "Manual",
                          "Label": "Година початку засідання",
                          "Entity": null,
                          "Search": [],
                          "Display": [],
                          "Filters": [],
                          "OrderBy": [],
                          "Required": false,
                          "DependsOn": [],
                          "MaxLength": 2,
                          "ValuePath": null,
                          "ValueType": "String",
                          "Description": []
                        },
                        "MeetingStartMinute": {
                          "Kind": "Manual",
                          "Label": "Хвилина початку засідання",
                          "Entity": null,
                          "Search": [],
                          "Display": [],
                          "Filters": [],
                          "OrderBy": [],
                          "Required": false,
                          "DependsOn": [],
                          "MaxLength": 2,
                          "ValuePath": null,
                          "ValueType": "String",
                          "Description": []
                        },
                        "MeetingEndHour": {
                          "Kind": "Manual",
                          "Label": "Година кінця засідання",
                          "Entity": null,
                          "Search": [],
                          "Display": [],
                          "Filters": [],
                          "OrderBy": [],
                          "Required": false,
                          "DependsOn": [],
                          "MaxLength": 2,
                          "ValuePath": null,
                          "ValueType": "String",
                          "Description": []
                        },
                        "MeetingEndMinute": {
                          "Kind": "Manual",
                          "Label": "Хвилина кінця засідання",
                          "Entity": null,
                          "Search": [],
                          "Display": [],
                          "Filters": [],
                          "OrderBy": [],
                          "Required": false,
                          "DependsOn": [],
                          "MaxLength": 2,
                          "ValuePath": null,
                          "ValueType": "String",
                          "Description": []
                        }
                      },
                      "HelperKeys": [
                        "SingleQualificationWorkProtocol"
                      ],
                      "DataSources": [
                        {
                          "Key": "TargetStudent",
                          "Entity": "Student",
                          "Filter": "Id == @0",
                          "Result": "One",
                          "OrderBy": [],
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
                          "FilterArgs": [
                            "StudentId"
                          ]
                        }
                      ],
                      "Description": "Сценарій для формування протоколу захисту кваліфікаційної роботи одного студента.",
                      "RequiredTableSources": [],
                      "RequiredScalarMappings": [
                        {
                          "Tag": "ProtocolNumber",
                          "Path": "Input.ProtocolNumber",
                          "Message": "Номер протоколу вводиться вручну."
                        },
                        {
                          "Tag": "MeetingDate",
                          "Path": "Input.MeetingDate",
                          "Message": "Дата засідання вводиться вручну."
                        },
                        {
                          "Tag": "MeetingYear",
                          "Path": "TargetStudent.Group.Year",
                          "Message": "Рік засідання береться з року групи вибраного студента."
                        },
                        {
                          "Tag": "CommissionOrderNumber",
                          "Path": "TargetStudent.Group.DiplomaExaminationCommission.OrderNumber",
                          "Message": "Номер наказу комісії береться з комісії групи вибраного студента."
                        },
                        {
                          "Tag": "EducationLevel",
                          "Path": "TargetStudent.Group.EducationLevel",
                          "Message": "Освітній рівень береться з групи вибраного студента."
                        },
                        {
                          "Tag": "SpecialtyLine",
                          "Path": "Computed.SpecialtyLine",
                          "Message": "Рядок спеціальності формується з коду та назви спеціальності."
                        },
                        {
                          "Tag": "EducationalProgram",
                          "Path": "TargetStudent.Group.Specialty.Name",
                          "Message": "Освітня програма береться з назви спеціальності групи вибраного студента."
                        },
                        {
                          "Tag": "MeetingStartHour",
                          "Path": "Input.MeetingStartHour",
                          "Message": "Година початку засідання вводиться вручну."
                        },
                        {
                          "Tag": "MeetingStartMinute",
                          "Path": "Input.MeetingStartMinute",
                          "Message": "Хвилина початку засідання вводиться вручну."
                        },
                        {
                          "Tag": "MeetingEndHour",
                          "Path": "Input.MeetingEndHour",
                          "Message": "Година кінця засідання вводиться вручну."
                        },
                        {
                          "Tag": "MeetingEndMinute",
                          "Path": "Input.MeetingEndMinute",
                          "Message": "Хвилина кінця засідання вводиться вручну."
                        },
                        {
                          "Tag": "StudentNameGenitive",
                          "Path": "TargetStudent.NameForms.Genitive",
                          "Message": "Ім'я студента в родовому відмінку береться з форм імені студента."
                        },
                        {
                          "Tag": "Topic",
                          "Path": "TargetStudent.QualificationWork.Topic",
                          "Message": "Тема береться з кваліфікаційної роботи студента."
                        },
                        {
                          "Tag": "CommissionHeadPresentLine",
                          "Path": "Computed.CommissionHeadPresentLine",
                          "Message": "Рядок присутності голови комісії формується сценарієм."
                        },
                        {
                          "Tag": "FirstMemberPresentLine",
                          "Path": "Computed.FirstMemberPresentLine",
                          "Message": "Рядок присутності першого члена комісії формується сценарієм."
                        },
                        {
                          "Tag": "SecondMemberPresentLine",
                          "Path": "Computed.SecondMemberPresentLine",
                          "Message": "Рядок присутності другого члена комісії формується сценарієм."
                        },
                        {
                          "Tag": "ThirdMemberPresentLine",
                          "Path": "Computed.ThirdMemberPresentLine",
                          "Message": "Рядок присутності третього члена комісії формується сценарієм."
                        },
                        {
                          "Tag": "SupervisorLine",
                          "Path": "Computed.SupervisorLine",
                          "Message": "Рядок керівника формується сценарієм."
                        },
                        {
                          "Tag": "ReviewerLine",
                          "Path": "Computed.ReviewerLine",
                          "Message": "Рядок рецензента формується сценарієм."
                        },
                        {
                          "Tag": "Question1AskedBy",
                          "Path": "Computed.Question1AskedBy",
                          "Message": "Автор першого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question1Text",
                          "Path": "Computed.Question1Text",
                          "Message": "Текст першого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question2AskedBy",
                          "Path": "Computed.Question2AskedBy",
                          "Message": "Автор другого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question2Text",
                          "Path": "Computed.Question2Text",
                          "Message": "Текст другого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question3AskedBy",
                          "Path": "Computed.Question3AskedBy",
                          "Message": "Автор третього питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question3Text",
                          "Path": "Computed.Question3Text",
                          "Message": "Текст третього питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question4AskedBy",
                          "Path": "Computed.Question4AskedBy",
                          "Message": "Автор четвертого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question4Text",
                          "Path": "Computed.Question4Text",
                          "Message": "Текст четвертого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question5AskedBy",
                          "Path": "Computed.Question5AskedBy",
                          "Message": "Автор п'ятого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "Question5Text",
                          "Path": "Computed.Question5Text",
                          "Message": "Текст п'ятого питання захисту формується сценарієм як скалярне значення."
                        },
                        {
                          "Tag": "StudentNameNominative",
                          "Path": "TargetStudent.NameForms.Nominative",
                          "Message": "Ім'я студента в називному відмінку береться з форм імені студента."
                        },
                        {
                          "Tag": "NationalGrade",
                          "Path": "TargetStudent.QualificationWork.NationalGrade",
                          "Message": "Національна оцінка береться з кваліфікаційної роботи."
                        },
                        {
                          "Tag": "EctsGrade",
                          "Path": "TargetStudent.QualificationWork.EctsGrade",
                          "Message": "Оцінка ECTS береться з кваліфікаційної роботи."
                        },
                        {
                          "Tag": "StudentNameDative",
                          "Path": "TargetStudent.NameForms.Dative",
                          "Message": "Ім'я студента в давальному відмінку береться з форм імені студента."
                        },
                        {
                          "Tag": "DiplomaHonors",
                          "Path": "TargetStudent.QualificationWork.HasDiplomaWithHonors",
                          "Message": "Ознака диплома з відзнакою береться з кваліфікаційної роботи."
                        },
                        {
                          "Tag": "CommissionHeadSignatureName",
                          "Path": "Computed.CommissionHeadSignatureName",
                          "Message": "Ім'я голови комісії для підпису формується сценарієм."
                        },
                        {
                          "Tag": "FirstMemberSignatureName",
                          "Path": "Computed.FirstMemberSignatureName",
                          "Message": "Ім'я першого члена комісії для підпису формується сценарієм."
                        },
                        {
                          "Tag": "SecondMemberSignatureName",
                          "Path": "Computed.SecondMemberSignatureName",
                          "Message": "Ім'я другого члена комісії для підпису формується сценарієм."
                        },
                        {
                          "Tag": "ThirdMemberSignatureName",
                          "Path": "Computed.ThirdMemberSignatureName",
                          "Message": "Ім'я третього члена комісії для підпису формується сценарієм."
                        },
                        {
                          "Tag": "SecretarySignatureName",
                          "Path": "Computed.SecretarySignatureName",
                          "Message": "Ім'я секретаря для підпису формується сценарієм."
                        }
                      ],
                      "RecommendedTableSources": [
                        {
                          "Key": "Computed.DefenceQuestions",
                          "Label": "Питання захисту",
                          "Entity": "Computed"
                        }
                      ]
                    }
                    $scenario$::jsonb
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "Title" = 'Single qualification work protocol',
                    "Description" = 'Protocol for one student qualification work defence with prebuilt protocol helper fields.',
                    "ScenarioJson" = jsonb_set(
                        jsonb_set(
                            "ScenarioJson",
                            '{Title}',
                            '"Single qualification work protocol"'::jsonb,
                            true),
                        '{Description}',
                        '"Protocol for one student qualification work defence with prebuilt protocol helper fields."'::jsonb,
                        true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }
    }
}
