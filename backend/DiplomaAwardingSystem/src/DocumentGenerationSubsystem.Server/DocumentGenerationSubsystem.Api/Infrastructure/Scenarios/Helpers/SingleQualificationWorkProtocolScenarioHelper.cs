using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Scenarios.Helpers;

public sealed class SingleQualificationWorkProtocolScenarioHelper(DbDocGenContext dbContext)
    : IDocumentScenarioHelper, IScopedService
{
    public string Key => "SingleQualificationWorkProtocol";

    public async Task<Result<IReadOnlyDictionary<string, object>>> BuildAsync(
        DocumentScenarioContext context,
        CancellationToken ct)
    {
        var studentIdResult = ParseRequiredInput<int>(context, "StudentId");
        if (studentIdResult.IsFailure)
        {
            return studentIdResult.ErrorDetails;
        }

        var student = await dbContext.Students
            .AsNoTracking()
            .Include(s => s.Group)
            .ThenInclude(g => g!.Specialty)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.CommissionHead)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.FirstMemberTeacher)
            .ThenInclude(t => t!.AcademicDegree)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.FirstMemberTeacher)
            .ThenInclude(t => t!.TeacherPosition)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.SecondMemberTeacher)
            .ThenInclude(t => t!.AcademicDegree)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.SecondMemberTeacher)
            .ThenInclude(t => t!.TeacherPosition)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.ThirdMemberTeacher)
            .ThenInclude(t => t!.AcademicDegree)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.ThirdMemberTeacher)
            .ThenInclude(t => t!.TeacherPosition)
            .Include(s => s.Group)
            .ThenInclude(g => g!.DiplomaExaminationCommission)
            .ThenInclude(dec => dec!.Secretary)
            .Include(s => s.QualificationWork)
            .ThenInclude(qw => qw!.Teacher)
            .ThenInclude(t => t!.AcademicDegree)
            .Include(s => s.QualificationWork)
            .ThenInclude(qw => qw!.Teacher)
            .ThenInclude(t => t!.TeacherPosition)
            .Include(s => s.QualificationWork)
            .ThenInclude(qw => qw!.Reviewer)
            .ThenInclude(t => t!.AcademicDegree)
            .Include(s => s.QualificationWork)
            .ThenInclude(qw => qw!.Reviewer)
            .ThenInclude(t => t!.TeacherPosition)
            .FirstOrDefaultAsync(s => s.Id == studentIdResult.Value, ct);

        if (student is null)
        {
            return ErrorDetails.Validation(
                "DocGen.StudentNotFound",
                "Selected student was not found.");
        }

        var qualificationWork = student.QualificationWork;
        var commission = student.Group?.DiplomaExaminationCommission;
        var computed = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["StudentNameNominative"] = student.NameForms.Nominative,
            ["StudentNameGenitive"] = student.NameForms.Genitive,
            ["StudentNameDative"] = student.NameForms.Dative,
            ["StudentSignatureName"] = student.NameForms.Signature,
            ["SupervisorLine"] = BuildTeacherWorkLine(qualificationWork?.Teacher),
            ["ReviewerLine"] = BuildReviewerLine(qualificationWork?.Reviewer),
            ["CommissionHeadPresentLine"] = BuildCommissionHeadPresentLine(commission?.CommissionHead),
            ["CommissionHeadSignatureName"] = commission?.CommissionHead?.NameForms.Signature ?? string.Empty,
            ["FirstMemberPresentLine"] = BuildTeacherPresentLine(commission?.FirstMemberTeacher),
            ["SecondMemberPresentLine"] = BuildTeacherPresentLine(commission?.SecondMemberTeacher),
            ["ThirdMemberPresentLine"] = BuildTeacherPresentLine(commission?.ThirdMemberTeacher),
            ["FirstMemberSignatureName"] = commission?.FirstMemberTeacher?.NameForms.Signature ?? string.Empty,
            ["SecondMemberSignatureName"] = commission?.SecondMemberTeacher?.NameForms.Signature ?? string.Empty,
            ["ThirdMemberSignatureName"] = commission?.ThirdMemberTeacher?.NameForms.Signature ?? string.Empty,
            ["SecretarySignatureName"] = commission?.Secretary?.FullName ?? string.Empty,
            ["DefenceQuestions"] = BuildQuestionRows(qualificationWork)
        };

        return computed;
    }

    private static Result<T> ParseRequiredInput<T>(DocumentScenarioContext context, string inputKey)
    {
        if (context.Configuration.Inputs is null || !context.Configuration.Inputs.TryGetValue(inputKey, out var input))
        {
            return ErrorDetails.Validation(
                "DocGen.ComputedInputMissing",
                $"Scenario helper requires input '{inputKey}'.");
        }

        if (!context.Parameters.TryGetValue(inputKey, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return ErrorDetails.Validation(
                "DocGen.ComputedInputValueMissing",
                $"Scenario helper requires selected value for input '{inputKey}'.");
        }

        var parsedValueResult = TemplateConfigurationReader.ParseInputValue(inputKey, input.ValueType, rawValue);
        if (parsedValueResult.IsFailure)
        {
            return parsedValueResult.ErrorDetails;
        }

        return parsedValueResult.Value is T typedValue
            ? typedValue
            : ErrorDetails.Validation(
                "DocGen.ComputedInputTypeMismatch",
                $"Scenario helper input '{inputKey}' has unexpected value type.");
    }

    private static string BuildTeacherPresentLine(Teacher? teacher)
    {
        if (teacher is null)
        {
            return string.Empty;
        }

        return JoinNonEmpty(
            teacher.NameForms.Nominative,
            teacher.AcademicDegree?.ShortName,
            teacher.TeacherPosition?.FullName);
    }

    private static string BuildTeacherWorkLine(Teacher? teacher)
    {
        if (teacher is null)
        {
            return string.Empty;
        }

        return JoinNonEmpty(
            teacher.TeacherPosition?.GenitiveShortName,
            teacher.AcademicDegree?.GenitiveShortName,
            teacher.NameForms.Genitive);
    }

    private static string BuildReviewerLine(Teacher? teacher)
    {
        if (teacher is null)
        {
            return string.Empty;
        }

        return "рецензент - " + JoinNonEmpty(
            teacher.AcademicDegree?.ShortName,
            teacher.TeacherPosition?.FullName,
            teacher.NameForms.Nominative);
    }

    private static string BuildCommissionHeadPresentLine(CommissionHead? head)
    {
        if (head is null)
        {
            return string.Empty;
        }

        return JoinNonEmpty(head.Position, head.Company, "/ " + head.NameForms.Nominative);
    }

    private static List<Dictionary<string, object>> BuildQuestionRows(QualificationWork? qualificationWork)
    {
        var rows = new List<Dictionary<string, object>>();
        var questions = qualificationWork?.DefenceQuestions.Take(5).ToList() ?? [];

        for (var i = 0; i < 5; i++)
        {
            var question = i < questions.Count ? questions[i] : null;
            rows.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Number"] = i + 1,
                ["AskedBy"] = question?.AskedBy ?? string.Empty,
                ["Text"] = question?.Text ?? string.Empty
            });
        }

        return rows;
    }

    private static string JoinNonEmpty(params string?[] values)
    {
        return string.Join(
            ", ",
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
    }
}
