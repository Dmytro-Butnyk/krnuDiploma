using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaAwardingSystem.MigrationsHost.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSingleProtocolScenarioMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diploma."DocumentConstructorScenarios"
                SET "ScenarioJson" = jsonb_set(
                    "ScenarioJson",
                    '{RequiredScalarMappings}',
                    $mappings$
                    [
                      { "Tag": "ProtocolNumber", "Path": "Input.ProtocolNumber", "Message": "Manual protocol number." },
                      { "Tag": "MeetingDate", "Path": "Input.MeetingDate", "Message": "Manual meeting date." },
                      { "Tag": "MeetingStartHour", "Path": "Computed.MeetingStartHour", "Message": "Computed from input MeetingStartTime." },
                      { "Tag": "MeetingStartMinute", "Path": "Computed.MeetingStartMinute", "Message": "Computed from input MeetingStartTime." },
                      { "Tag": "MeetingEndHour", "Path": "Computed.MeetingEndHour", "Message": "Computed from input MeetingEndTime." },
                      { "Tag": "MeetingEndMinute", "Path": "Computed.MeetingEndMinute", "Message": "Computed from input MeetingEndTime." },

                      { "Tag": "CommissionOrderNumber", "Path": "TargetStudent.Group.DiplomaExaminationCommission.OrderNumber", "Message": "Commission order number from selected student's group commission." },
                      { "Tag": "EducationLevel", "Path": "TargetStudent.Group.EducationLevel", "Message": "Education level from selected student's group." },
                      { "Tag": "SpecialtyLine", "Path": "Computed.SpecialtyLine", "Message": "Computed from specialty code and name." },
                      { "Tag": "EducationalProgram", "Path": "Input.EducationalProgram", "Message": "Manual educational program." },
                      { "Tag": "EducationQualification", "Path": "Input.EducationQualification", "Message": "Manual education qualification." },
                      { "Tag": "ProfessionalQualification", "Path": "Input.ProfessionalQualification", "Message": "Manual professional qualification." },

                      { "Tag": "StudentNameNominative", "Path": "TargetStudent.NameForms.Nominative", "Message": "Student nominative name from name forms." },
                      { "Tag": "StudentNameGenitive", "Path": "TargetStudent.NameForms.Genitive", "Message": "Student genitive name from name forms." },
                      { "Tag": "StudentNameDative", "Path": "TargetStudent.NameForms.Dative", "Message": "Student dative name from name forms." },
                      { "Tag": "Topic", "Path": "TargetStudent.QualificationWork.Topic", "Message": "Qualification work topic." },

                      { "Tag": "CommissionHeadPresentLine", "Path": "Computed.CommissionHeadPresentLine", "Message": "Computed commission head present line." },
                      { "Tag": "FirstMemberPresentLine", "Path": "Computed.FirstMemberPresentLine", "Message": "Computed first commission member present line." },
                      { "Tag": "SecondMemberPresentLine", "Path": "Computed.SecondMemberPresentLine", "Message": "Computed second commission member present line." },
                      { "Tag": "ThirdMemberPresentLine", "Path": "Computed.ThirdMemberPresentLine", "Message": "Computed third commission member present line." },
                      { "Tag": "SupervisorLine", "Path": "Computed.SupervisorLine", "Message": "Computed supervisor line." },
                      { "Tag": "ReviewerLine", "Path": "Computed.ReviewerLine", "Message": "Computed reviewer line." },

                      { "Tag": "Question1AskedBy", "Path": "Computed.Question1AskedBy", "Message": "Computed first defence question author." },
                      { "Tag": "Question1Text", "Path": "Computed.Question1Text", "Message": "Computed first defence question text." },
                      { "Tag": "Question2AskedBy", "Path": "Computed.Question2AskedBy", "Message": "Computed second defence question author." },
                      { "Tag": "Question2Text", "Path": "Computed.Question2Text", "Message": "Computed second defence question text." },
                      { "Tag": "Question3AskedBy", "Path": "Computed.Question3AskedBy", "Message": "Computed third defence question author." },
                      { "Tag": "Question3Text", "Path": "Computed.Question3Text", "Message": "Computed third defence question text." },
                      { "Tag": "Question4AskedBy", "Path": "Computed.Question4AskedBy", "Message": "Computed fourth defence question author." },
                      { "Tag": "Question4Text", "Path": "Computed.Question4Text", "Message": "Computed fourth defence question text." },
                      { "Tag": "Question5AskedBy", "Path": "Computed.Question5AskedBy", "Message": "Computed fifth defence question author." },
                      { "Tag": "Question5Text", "Path": "Computed.Question5Text", "Message": "Computed fifth defence question text." },

                      { "Tag": "ReportDurationMinutes", "Path": "Input.ReportDurationMinutes", "Message": "Manual report duration in minutes." },
                      { "Tag": "NationalGrade", "Path": "TargetStudent.QualificationWork.NationalGrade", "Message": "National grade from qualification work." },
                      { "Tag": "EctsGrade", "Path": "TargetStudent.QualificationWork.EctsGrade", "Message": "ECTS grade from qualification work." },
                      { "Tag": "DiplomaHonors", "Path": "TargetStudent.QualificationWork.HasDiplomaWithHonors", "Message": "Diploma honors flag formatted by generator." },
                      { "Tag": "CompetenceNote", "Path": "Input.CompetenceNote", "Message": "Manual competence note." },

                      { "Tag": "CommissionHeadSignatureName", "Path": "Computed.CommissionHeadSignatureName", "Message": "Computed commission head signature name." },
                      { "Tag": "FirstMemberSignatureName", "Path": "Computed.FirstMemberSignatureName", "Message": "Computed first commission member signature name." },
                      { "Tag": "SecondMemberSignatureName", "Path": "Computed.SecondMemberSignatureName", "Message": "Computed second commission member signature name." },
                      { "Tag": "ThirdMemberSignatureName", "Path": "Computed.ThirdMemberSignatureName", "Message": "Computed third commission member signature name." },
                      { "Tag": "SecretarySignatureName", "Path": "Computed.SecretarySignatureName", "Message": "Computed secretary signature name." }
                    ]
                    $mappings$::jsonb,
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
                    '{RequiredScalarMappings}',
                    $mappings$
                    [
                      { "Tag": "StudentNameGenitive", "Path": "Computed.StudentNameGenitive", "Message": "Use computed student genitive name." },
                      { "Tag": "StudentNameDative", "Path": "Computed.StudentNameDative", "Message": "Use computed student dative name." },
                      { "Tag": "SupervisorLine", "Path": "Computed.SupervisorLine", "Message": "Use computed supervisor line." },
                      { "Tag": "ReviewerLine", "Path": "Computed.ReviewerLine", "Message": "Use computed reviewer line." }
                    ]
                    $mappings$::jsonb,
                    true)
                WHERE "Code" = 'single-qualification-work-protocol';
                """);
        }
    }
}
