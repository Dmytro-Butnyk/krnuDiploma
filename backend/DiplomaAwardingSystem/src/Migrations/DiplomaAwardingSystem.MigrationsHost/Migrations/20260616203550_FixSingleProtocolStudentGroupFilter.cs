using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    public partial class FixSingleProtocolStudentGroupFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "ScenarioJson" = jsonb_set(
                    "ScenarioJson",
                    '{Inputs,StudentId,Filters}',
                    '[{ "Property": "GroupId", "Operator": "Equals", "Input": "GroupId" }]'::jsonb,
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
                    '{Inputs,StudentId,Filters}',
                    '[{ "Property": "Student.GroupId", "Operator": "Equals", "Input": "GroupId" }]'::jsonb,
                    true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }
    }
}
