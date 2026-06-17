using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    public partial class AddProtocolFieldsAndCommissionConsultants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewerScore",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "SupervisorScore",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.AddColumn<int>(
                name: "DurationOfDefenceMinutes",
                schema: "diploma",
                table: "QualificationWorks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PresentationSheets",
                schema: "diploma",
                table: "QualificationWorks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProtocolNumber",
                schema: "diploma",
                table: "QualificationWorks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkSheets",
                schema: "diploma",
                table: "QualificationWorks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingEnd",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "MeetingStart",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "SecondConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_FirstConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "FirstConsultantId");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaExaminationCommissions_SecondConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecondConsultantId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_FirstConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "FirstConsultantId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_SecondConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions",
                column: "SecondConsultantId",
                principalSchema: "diploma",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "ScenarioJson" = jsonb_set(
                    jsonb_set(
                        jsonb_set(
                            "ScenarioJson",
                            '{Inputs}',
                            (
                                jsonb_set(
                                    jsonb_set(
                                        ("ScenarioJson"->'Inputs')
                                            - 'ProtocolNumber'
                                            - 'MeetingStartHour'
                                            - 'MeetingStartMinute'
                                            - 'MeetingEndHour'
                                            - 'MeetingEndMinute',
                                        '{StudentId,DependsOn}',
                                        '["GroupId"]'::jsonb,
                                        true),
                                    '{StudentId,Filters}',
                                    '[{ "Property": "Student.GroupId", "Operator": "Equals", "Input": "GroupId" }]'::jsonb,
                                    true)
                                || $groupInput$
                                {
                                  "GroupId": {
                                    "Kind": "EntitySelect",
                                    "Label": "Група",
                                    "Entity": "Group",
                                    "Search": [
                                      "Name"
                                    ],
                                    "Display": [
                                      "Name"
                                    ],
                                    "Filters": [],
                                    "OrderBy": [
                                      "Name"
                                    ],
                                    "Required": true,
                                    "DependsOn": [],
                                    "MaxLength": null,
                                    "ValuePath": null,
                                    "ValueType": "Int",
                                    "Description": []
                                  }
                                }
                                $groupInput$::jsonb
                            ),
                            true),
                        '{DataSources}',
                        (
                            SELECT jsonb_agg(
                                CASE
                                    WHEN source->>'Key' = 'TargetStudent' THEN
                                        jsonb_set(
                                            source,
                                            '{Includes}',
                                            (source->'Includes') || $includes$
                                            [
                                              "Group.DiplomaExaminationCommission.FirstConsultant.AcademicDegree",
                                              "Group.DiplomaExaminationCommission.FirstConsultant.TeacherPosition",
                                              "Group.DiplomaExaminationCommission.SecondConsultant.AcademicDegree",
                                              "Group.DiplomaExaminationCommission.SecondConsultant.TeacherPosition"
                                            ]
                                            $includes$::jsonb,
                                            true)
                                    ELSE source
                                END
                                ORDER BY ord)
                            FROM jsonb_array_elements("ScenarioJson"->'DataSources') WITH ORDINALITY AS sources(source, ord)
                        ),
                        true),
                    '{RequiredScalarMappings}',
                    (
                        (
                            SELECT jsonb_agg(
                                CASE
                                    WHEN mapping->>'Tag' = 'ProtocolNumber' THEN
                                        jsonb_build_object(
                                            'Tag', 'ProtocolNumber',
                                            'Path', 'Computed.ProtocolNumber',
                                            'Message', 'Номер протоколу береться з кваліфікаційної роботи студента.')
                                    WHEN mapping->>'Tag' = 'MeetingStartHour' THEN
                                        jsonb_build_object(
                                            'Tag', 'MeetingStartHour',
                                            'Path', 'Computed.MeetingStartHour',
                                            'Message', 'Година початку засідання береться з часу початку засідання комісії.')
                                    WHEN mapping->>'Tag' = 'MeetingStartMinute' THEN
                                        jsonb_build_object(
                                            'Tag', 'MeetingStartMinute',
                                            'Path', 'Computed.MeetingStartMinute',
                                            'Message', 'Хвилина початку засідання береться з часу початку засідання комісії.')
                                    WHEN mapping->>'Tag' = 'MeetingEndHour' THEN
                                        jsonb_build_object(
                                            'Tag', 'MeetingEndHour',
                                            'Path', 'Computed.MeetingEndHour',
                                            'Message', 'Година кінця засідання береться з часу кінця засідання комісії.')
                                    WHEN mapping->>'Tag' = 'MeetingEndMinute' THEN
                                        jsonb_build_object(
                                            'Tag', 'MeetingEndMinute',
                                            'Path', 'Computed.MeetingEndMinute',
                                            'Message', 'Хвилина кінця засідання береться з часу кінця засідання комісії.')
                                    ELSE mapping
                                END
                                ORDER BY ord)
                            FROM jsonb_array_elements("ScenarioJson"->'RequiredScalarMappings') WITH ORDINALITY AS mappings(mapping, ord)
                        ) || $mappings$
                        [
                          {
                            "Tag": "DurationMinutes",
                            "Path": "Computed.DurationMinutes",
                            "Message": "Тривалість захисту береться з кваліфікаційної роботи студента."
                          },
                          {
                            "Tag": "PresentationSheets",
                            "Path": "Computed.PresentationSheets",
                            "Message": "Кількість сторінок презентації береться з кваліфікаційної роботи студента."
                          },
                          {
                            "Tag": "WorkSheets",
                            "Path": "Computed.WorkSheets",
                            "Message": "Кількість сторінок пояснювальної записки береться з кваліфікаційної роботи студента."
                          },
                          {
                            "Tag": "Consultant1Line",
                            "Path": "Computed.Consultant1Line",
                            "Message": "Рядок першого консультанта формується сценарієм."
                          },
                          {
                            "Tag": "Consultant2Line",
                            "Path": "Computed.Consultant2Line",
                            "Message": "Рядок другого консультанта формується сценарієм."
                          },
                          {
                            "Tag": "CompetencyLevel",
                            "Path": "Computed.CompetencyLevel",
                            "Message": "Рівень компетенції розраховується за оцінкою комісії."
                          }
                        ]
                        $mappings$::jsonb
                    ),
                    true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "ScenarioJson" = jsonb_set(
                    jsonb_set(
                        jsonb_set(
                            "ScenarioJson",
                            '{Inputs}',
                            jsonb_set(
                                jsonb_set(
                                    ("ScenarioJson"->'Inputs') - 'GroupId' || $inputs$
                                    {
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
                                    }
                                    $inputs$::jsonb,
                                    '{StudentId,DependsOn}',
                                    '[]'::jsonb,
                                    true),
                                '{StudentId,Filters}',
                                '[]'::jsonb,
                                true),
                            true),
                        '{DataSources}',
                        (
                            SELECT jsonb_agg(
                                CASE
                                    WHEN source->>'Key' = 'TargetStudent' THEN
                                        jsonb_set(
                                            source,
                                            '{Includes}',
                                            (
                                                SELECT COALESCE(jsonb_agg(include), '[]'::jsonb)
                                                FROM jsonb_array_elements(source->'Includes') AS includes(include)
                                                WHERE include #>> '{}' NOT IN (
                                                    'Group.DiplomaExaminationCommission.FirstConsultant.AcademicDegree',
                                                    'Group.DiplomaExaminationCommission.FirstConsultant.TeacherPosition',
                                                    'Group.DiplomaExaminationCommission.SecondConsultant.AcademicDegree',
                                                    'Group.DiplomaExaminationCommission.SecondConsultant.TeacherPosition')
                                            ),
                                            true)
                                    ELSE source
                                END
                                ORDER BY ord)
                            FROM jsonb_array_elements("ScenarioJson"->'DataSources') WITH ORDINALITY AS sources(source, ord)
                        ),
                        true),
                    '{RequiredScalarMappings}',
                    (
                        SELECT jsonb_agg(
                            CASE
                                WHEN mapping->>'Tag' = 'ProtocolNumber' THEN
                                    jsonb_build_object(
                                        'Tag', 'ProtocolNumber',
                                        'Path', 'Input.ProtocolNumber',
                                        'Message', 'Номер протоколу вводиться вручну.')
                                WHEN mapping->>'Tag' = 'MeetingStartHour' THEN
                                    jsonb_build_object(
                                        'Tag', 'MeetingStartHour',
                                        'Path', 'Input.MeetingStartHour',
                                        'Message', 'Година початку засідання вводиться вручну.')
                                WHEN mapping->>'Tag' = 'MeetingStartMinute' THEN
                                    jsonb_build_object(
                                        'Tag', 'MeetingStartMinute',
                                        'Path', 'Input.MeetingStartMinute',
                                        'Message', 'Хвилина початку засідання вводиться вручну.')
                                WHEN mapping->>'Tag' = 'MeetingEndHour' THEN
                                    jsonb_build_object(
                                        'Tag', 'MeetingEndHour',
                                        'Path', 'Input.MeetingEndHour',
                                        'Message', 'Година кінця засідання вводиться вручну.')
                                WHEN mapping->>'Tag' = 'MeetingEndMinute' THEN
                                    jsonb_build_object(
                                        'Tag', 'MeetingEndMinute',
                                        'Path', 'Input.MeetingEndMinute',
                                        'Message', 'Хвилина кінця засідання вводиться вручну.')
                                ELSE mapping
                            END
                            ORDER BY ord)
                        FROM jsonb_array_elements("ScenarioJson"->'RequiredScalarMappings') WITH ORDINALITY AS mappings(mapping, ord)
                        WHERE mapping->>'Tag' NOT IN (
                            'DurationMinutes',
                            'PresentationSheets',
                            'WorkSheets',
                            'Consultant1Line',
                            'Consultant2Line',
                            'CompetencyLevel')
                    ),
                    true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_FirstConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaExaminationCommissions_Teachers_SecondConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_FirstConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropIndex(
                name: "IX_DiplomaExaminationCommissions_SecondConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "DurationOfDefenceMinutes",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "PresentationSheets",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "ProtocolNumber",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "WorkSheets",
                schema: "diploma",
                table: "QualificationWorks");

            migrationBuilder.DropColumn(
                name: "FirstConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "MeetingEnd",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "MeetingStart",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.DropColumn(
                name: "SecondConsultantId",
                schema: "diploma",
                table: "DiplomaExaminationCommissions");

            migrationBuilder.AddColumn<int>(
                name: "ReviewerScore",
                schema: "diploma",
                table: "QualificationWorks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorScore",
                schema: "diploma",
                table: "QualificationWorks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
