using Core.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DbDocGenContext))]
    [Migration("20260616013000_UseDefenceDateForSingleProtocolMeetingDate")]
    public partial class UseDefenceDateForSingleProtocolMeetingDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "ScenarioJson" = jsonb_set(
                    jsonb_set(
                        "ScenarioJson",
                        '{Inputs}',
                        ("ScenarioJson"->'Inputs') - 'MeetingDate',
                        true),
                    '{RequiredScalarMappings}',
                    (
                        SELECT jsonb_agg(
                            CASE
                                WHEN mapping->>'Tag' = 'MeetingDate' THEN
                                    jsonb_build_object(
                                        'Tag', 'MeetingDate',
                                        'Path', 'Computed.MeetingDate',
                                        'Message', 'Дата засідання береться з дати захисту кваліфікаційної роботи та формується як день і місяць.')
                                ELSE mapping
                            END
                            ORDER BY ord)
                        FROM jsonb_array_elements("ScenarioJson"->'RequiredScalarMappings') WITH ORDINALITY AS mappings(mapping, ord)
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
                        "ScenarioJson",
                        '{Inputs}',
                        ("ScenarioJson"->'Inputs') || $input$
                        {
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
                          }
                        }
                        $input$::jsonb,
                        true),
                    '{RequiredScalarMappings}',
                    (
                        SELECT jsonb_agg(
                            CASE
                                WHEN mapping->>'Tag' = 'MeetingDate' THEN
                                    jsonb_build_object(
                                        'Tag', 'MeetingDate',
                                        'Path', 'Input.MeetingDate',
                                        'Message', 'Дата засідання вводиться вручну.')
                                ELSE mapping
                            END
                            ORDER BY ord)
                        FROM jsonb_array_elements("ScenarioJson"->'RequiredScalarMappings') WITH ORDINALITY AS mappings(mapping, ord)
                    ),
                    true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }
    }
}
