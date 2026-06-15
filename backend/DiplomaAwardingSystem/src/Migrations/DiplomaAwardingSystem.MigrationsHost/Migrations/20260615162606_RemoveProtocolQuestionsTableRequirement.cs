using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProtocolQuestionsTableRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "ScenarioJson" = jsonb_set(
                    "ScenarioJson",
                    '{RequiredTableSources}',
                    '[]'::jsonb,
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
                    "ScenarioJson",
                    '{RequiredTableSources}',
                    $sources$
                    [
                      {
                        "SourceArray": "Computed.DefenceQuestions",
                        "Message": "Use computed defence questions table source."
                      }
                    ]
                    $sources$::jsonb,
                    true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }
    }
}
