using System.Globalization;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using DocumentGenerationSubsystem.Api.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Scenarios.Helpers;

public sealed class ProtocolsNumbersScenarioHelper(DbDocGenContext dbContext)
    : IDocumentScenarioHelper, IScopedService
{
    public string Key => "ProtocolsNumbers";

    public async Task<Result<IReadOnlyDictionary<string, object>>> BuildAsync(
        DocumentScenarioContext context,
        CancellationToken ct)
    {
        if (!RequiresComputedScalar(context.Configuration, "ProtocolsNumbers"))
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        var groupIdResult = ParseRequiredComputedInput<int>(context, "GroupId");
        if (groupIdResult.IsFailure)
        {
            return groupIdResult.ErrorDetails;
        }

        var defenceDateResult = ParseRequiredComputedInput<DateOnly>(context, "DefenceDate");
        if (defenceDateResult.IsFailure)
        {
            return defenceDateResult.ErrorDetails;
        }

        var groupId = groupIdResult.Value;
        var defenceDate = defenceDateResult.Value;

        var students = await dbContext.Students
            .AsNoTracking()
            .Where(student => student.GroupId == groupId
                              && student.QualificationWork != null
                              && student.QualificationWork.DefenceDate != null
                              && student.QualificationWork.CommissionScore >= 60)
            .Select(student => new
            {
                student.FullName,
                DefenceDate = student.QualificationWork!.DefenceDate!.Value
            })
            .OrderBy(student => student.DefenceDate)
            .ThenBy(student => student.FullName)
            .ToListAsync(ct);

        var firstNumber = 0;
        var lastNumber = 0;

        for (var index = 0; index < students.Count; index++)
        {
            if (students[index].DefenceDate != defenceDate)
            {
                continue;
            }

            var protocolNumber = index + 1;
            if (firstNumber == 0)
            {
                firstNumber = protocolNumber;
            }

            lastNumber = protocolNumber;
        }

        if (firstNumber == 0)
        {
            return ErrorDetails.Validation(
                "DocGen.ComputedProtocolsNumbers.Empty",
                "Cannot compute protocol numbers because no students with score 60+ were found for the selected group and defence date.");
        }

        var value = firstNumber == lastNumber
            ? firstNumber.ToString(CultureInfo.InvariantCulture)
            : string.Concat(
                firstNumber.ToString(CultureInfo.InvariantCulture),
                "-",
                lastNumber.ToString(CultureInfo.InvariantCulture));

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ProtocolsNumbers"] = value
        };
    }

    private static Result<T> ParseRequiredComputedInput<T>(
        DocumentScenarioContext context,
        string inputKey)
    {
        if (context.Configuration.Inputs is null || !context.Configuration.Inputs.TryGetValue(inputKey, out var input))
        {
            return ErrorDetails.Validation(
                "DocGen.ComputedInputMissing",
                $"Computed field requires input '{inputKey}'.");
        }

        if (!context.Parameters.TryGetValue(inputKey, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return ErrorDetails.Validation(
                "DocGen.ComputedInputValueMissing",
                $"Computed field requires selected value for input '{inputKey}'.");
        }

        var parsedValueResult = TemplateConfigurationReader.ParseInputValue(inputKey, input.ValueType, rawValue);
        if (parsedValueResult.IsFailure)
        {
            return parsedValueResult.ErrorDetails;
        }

        if (parsedValueResult.Value is T typedValue)
        {
            return typedValue;
        }

        return ErrorDetails.Validation(
            "DocGen.ComputedInputTypeMismatch",
            $"Computed field input '{inputKey}' has unexpected value type.");
    }

    private static bool RequiresComputedScalar(TemplateConfiguration config, string key)
    {
        return config.Mapping?.Scalars is not null
               && config.Mapping.Scalars.Values.Any(path =>
                   string.Equals(path, $"Computed.{key}", StringComparison.OrdinalIgnoreCase));
    }
}
