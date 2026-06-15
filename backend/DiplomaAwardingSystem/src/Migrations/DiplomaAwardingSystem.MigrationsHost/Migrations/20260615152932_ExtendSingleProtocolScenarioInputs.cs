using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    public partial class ExtendSingleProtocolScenarioInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "ScenarioJson" = jsonb_set(
                    "ScenarioJson",
                    '{Inputs}',
                    ("ScenarioJson"->'Inputs') || $inputs$
                    {
                      "EducationalProgram": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Educational program",
                        "Required": true,
                        "Entity": null,
                        "ValuePath": null,
                        "DependsOn": [],
                        "Filters": [],
                        "Display": [],
                        "Description": [],
                        "Search": [],
                        "OrderBy": [],
                        "MaxLength": 256
                      },
                      "EducationQualification": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Education qualification",
                        "Required": true,
                        "Entity": null,
                        "ValuePath": null,
                        "DependsOn": [],
                        "Filters": [],
                        "Display": [],
                        "Description": [],
                        "Search": [],
                        "OrderBy": [],
                        "MaxLength": 256
                      },
                      "ProfessionalQualification": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Professional qualification",
                        "Required": false,
                        "Entity": null,
                        "ValuePath": null,
                        "DependsOn": [],
                        "Filters": [],
                        "Display": [],
                        "Description": [],
                        "Search": [],
                        "OrderBy": [],
                        "MaxLength": 256
                      },
                      "ConsultantLine1": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Consultant line 1",
                        "Required": false,
                        "Entity": null,
                        "ValuePath": null,
                        "DependsOn": [],
                        "Filters": [],
                        "Display": [],
                        "Description": [],
                        "Search": [],
                        "OrderBy": [],
                        "MaxLength": 500
                      },
                      "ConsultantLine2": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Consultant line 2",
                        "Required": false,
                        "Entity": null,
                        "ValuePath": null,
                        "DependsOn": [],
                        "Filters": [],
                        "Display": [],
                        "Description": [],
                        "Search": [],
                        "OrderBy": [],
                        "MaxLength": 500
                      },
                      "SummaryLanguage": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Summary language",
                        "Required": true,
                        "Entity": null,
                        "ValuePath": null,
                        "DependsOn": [],
                        "Filters": [],
                        "Display": [],
                        "Description": [],
                        "Search": [],
                        "OrderBy": [],
                        "MaxLength": 100
                      },
                      "PresentationSheets": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Presentation sheets",
                        "Required": false,
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
                      "ReportDurationMinutes": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Report duration minutes",
                        "Required": false,
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
                      "CompetenceNote": {
                        "Kind": "Manual",
                        "ValueType": "String",
                        "Label": "Competence note",
                        "Required": false,
                        "Entity": null,
                        "ValuePath": null,
                        "DependsOn": [],
                        "Filters": [],
                        "Display": [],
                        "Description": [],
                        "Search": [],
                        "OrderBy": [],
                        "MaxLength": 500
                      }
                    }
                    $inputs$::jsonb,
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
                    '{Inputs}',
                    ("ScenarioJson"->'Inputs')
                        - 'EducationalProgram'
                        - 'EducationQualification'
                        - 'ProfessionalQualification'
                        - 'ConsultantLine1'
                        - 'ConsultantLine2'
                        - 'SummaryLanguage'
                        - 'PresentationSheets'
                        - 'ReportDurationMinutes'
                        - 'CompetenceNote',
                    true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }
    }
}
