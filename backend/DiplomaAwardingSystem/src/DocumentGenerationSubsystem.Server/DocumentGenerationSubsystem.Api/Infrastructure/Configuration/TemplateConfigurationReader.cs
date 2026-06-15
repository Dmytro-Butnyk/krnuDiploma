using System.Globalization;
using System.Text.Json;
using Core.Domain.ResultPattern;
using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using DocumentGenerationSubsystem.Api.Infrastructure.Security;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Configuration;

internal static class TemplateConfigurationReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Result<TemplateConfiguration> Parse(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return ErrorDetails.Validation(
                "DocGen.ConfigurationMissing",
                "Template configuration is required.");
        }

        try
        {
            var configuration = JsonSerializer.Deserialize<TemplateConfiguration>(configurationJson, JsonOptions);
            if (configuration is null)
            {
                return ErrorDetails.Validation(
                    "DocGen.InvalidConfig",
                    "Failed to parse template configuration.");
            }

            return Validate(configuration);
        }
        catch (JsonException)
        {
            return ErrorDetails.Validation(
                "DocGen.InvalidConfig",
                "Failed to parse template configuration.");
        }
    }

    public static Result<Dictionary<string, object?>> BuildInputContext(
        TemplateConfiguration configuration,
        IReadOnlyDictionary<string, string> parameters)
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (configuration.Inputs is null || configuration.Inputs.Count == 0)
        {
            return context;
        }

        foreach (var (key, input) in configuration.Inputs)
        {
            parameters.TryGetValue(key, out var rawValue);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (input.Required)
                {
                    return ErrorDetails.Validation(
                        "DocGen.MissingInput",
                        $"Missing required input: '{key}'.");
                }

                context[key] = null;
                continue;
            }

            if (input.MaxLength is > 0 && rawValue.Length > input.MaxLength.Value)
            {
                return ErrorDetails.Validation(
                    "DocGen.InputTooLong",
                    $"Input '{key}' cannot exceed {input.MaxLength.Value} characters.");
            }

            var parseResult = ParseInputValue(key, input.ValueType, rawValue);
            if (parseResult.IsFailure)
            {
                return parseResult.ErrorDetails;
            }

            context[key] = ShouldPreserveRawValue(input.ValueType) ? rawValue.Trim() : parseResult.Value;
        }

        return context;
    }

    public static Result<object?> ParseInputValue(string key, string? valueType, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return ErrorDetails.Validation(
                "DocGen.EmptyInput",
                $"Input '{key}' cannot be empty.");
        }

        var value = rawValue.Trim();
        switch (valueType)
        {
            case InputValueTypes.String or null or "":
                return value;

            case InputValueTypes.Int:
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)
                    ? intValue
                    : InvalidInputType(key, InputValueTypes.Int);

            case InputValueTypes.Long:
                return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue)
                    ? longValue
                    : InvalidInputType(key, InputValueTypes.Long);

            case InputValueTypes.Guid:
                return Guid.TryParse(value, out var guidValue)
                    ? guidValue
                    : InvalidInputType(key, InputValueTypes.Guid);

            case InputValueTypes.Bool:
                return bool.TryParse(value, out var boolValue)
                    ? boolValue
                    : InvalidInputType(key, InputValueTypes.Bool);

            case InputValueTypes.Date:
                return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue)
                    ? dateValue
                    : InvalidInputType(key, InputValueTypes.Date);

            case InputValueTypes.DateTime:
                return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTimeValue)
                    ? dateTimeValue
                    : InvalidInputType(key, InputValueTypes.DateTime);

            case InputValueTypes.Decimal:
                return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)
                    ? decimalValue
                    : InvalidInputType(key, InputValueTypes.Decimal);

            default:
                return ErrorDetails.Validation(
                    "DocGen.UnsupportedInputType",
                    $"Input '{key}' has unsupported value type '{valueType}'.");
        }
    }

    private static Result<TemplateConfiguration> Validate(TemplateConfiguration configuration)
    {
        if (configuration.ConfigurationVersion != TemplateConfigurationVersions.Current)
        {
            return ErrorDetails.Validation(
                "DocGen.UnsupportedConfigurationVersion",
                $"Template configuration version must be {TemplateConfigurationVersions.Current}.");
        }

        if (configuration.Mapping is null)
        {
            return ErrorDetails.Validation(
                "DocGen.MappingMissing",
                "Template configuration mapping is required.");
        }

        var mappingResult = ValidateMapping(configuration.Mapping);
        if (mappingResult.IsFailure)
        {
            return mappingResult.ErrorDetails;
        }

        if (configuration.Inputs is not null)
        {
            foreach (var (key, input) in configuration.Inputs)
            {
                var inputResult = ValidateInput(key, input, configuration.Inputs);
                if (inputResult.IsFailure)
                {
                    return inputResult.ErrorDetails;
                }
            }
        }

        if (configuration.DataSources is not null)
        {
            foreach (var source in configuration.DataSources)
            {
                if (!DocumentGenerationAllowedEntities.Registry.ContainsKey(source.Entity))
                {
                    return ErrorDetails.Forbidden(
                        "DocGen.UnauthorizedEntity",
                        $"Unknown or not allowed entity '{source.Entity}'.");
                }

                if (DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(source.Filter))
                {
                    return ProtectedPropertyError($"Data source '{source.Key}' filter references a protected property.");
                }

                if (DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(source.OrderBy))
                {
                    return ProtectedPropertyError($"Data source '{source.Key}' order by references a protected property.");
                }

                if (!string.IsNullOrWhiteSpace(source.Result)
                    && !string.Equals(source.Result, DataSourceResults.One, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(source.Result, DataSourceResults.Many, StringComparison.OrdinalIgnoreCase))
                {
                    return ErrorDetails.Validation(
                        "DocGen.UnsupportedDataSourceResult",
                        $"Data source '{source.Key}' has unsupported result mode '{source.Result}'.");
                }

                if (source.FilterArgs is null)
                {
                    continue;
                }

                foreach (var filterArg in source.FilterArgs)
                {
                    if (configuration.Inputs is null || !configuration.Inputs.ContainsKey(filterArg))
                    {
                        return ErrorDetails.Validation(
                            "DocGen.FilterArgInputMissing",
                            $"Data source '{source.Key}' uses filter argument '{filterArg}', but this input is not defined.");
                    }
                }
            }
        }

        return configuration;
    }

    private static Result ValidateInput(
        string key,
        InputConfig input,
        IReadOnlyDictionary<string, InputConfig> allInputs)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return ErrorDetails.Validation("DocGen.InvalidInputKey", "Input key cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(input.Kind))
        {
            return ErrorDetails.Validation("DocGen.InputKindMissing", $"Input '{key}' kind is required.");
        }

        if (string.IsNullOrWhiteSpace(input.ValueType))
        {
            return ErrorDetails.Validation("DocGen.InputValueTypeMissing", $"Input '{key}' value type is required.");
        }

        if (!IsSupportedValueType(input.ValueType))
        {
            return ErrorDetails.Validation(
                "DocGen.UnsupportedInputType",
                $"Input '{key}' has unsupported value type '{input.ValueType}'.");
        }

        if (string.Equals(input.Kind, InputKinds.EntitySelect, StringComparison.OrdinalIgnoreCase)
            || string.Equals(input.Kind, InputKinds.ValueSelect, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(input.Entity))
            {
                return ErrorDetails.Validation("DocGen.InputEntityMissing", $"Input '{key}' must define Entity.");
            }

            if (!DocumentGenerationAllowedEntities.Registry.ContainsKey(input.Entity))
            {
                return ErrorDetails.Forbidden(
                    "DocGen.UnauthorizedEntity",
                    $"Unknown or not allowed entity '{input.Entity}'.");
            }

            if (string.Equals(input.Kind, InputKinds.ValueSelect, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(input.ValuePath))
            {
                return ErrorDetails.Validation("DocGen.InputValuePathMissing", $"Value select input '{key}' must define ValuePath.");
            }

            if (DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(input.ValuePath)
                || DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(input.Display)
                || DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(input.Description)
                || DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(input.Search)
                || DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(input.OrderBy))
            {
                return ProtectedPropertyError($"Input '{key}' references a protected property.");
            }
        }
        else if (!string.Equals(input.Kind, InputKinds.Manual, StringComparison.OrdinalIgnoreCase))
        {
            return ErrorDetails.Validation(
                "DocGen.UnsupportedInputKind",
                $"Input '{key}' has unsupported kind '{input.Kind}'.");
        }

        if (input.DependsOn is not null)
        {
            foreach (var dependency in input.DependsOn)
            {
                if (!allInputs.ContainsKey(dependency))
                {
                    return ErrorDetails.Validation(
                        "DocGen.InputDependencyMissing",
                        $"Input '{key}' depends on unknown input '{dependency}'.");
                }
            }
        }

        if (input.Filters is not null)
        {
            foreach (var filter in input.Filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Property))
                {
                    return ErrorDetails.Validation(
                        "DocGen.InputFilterInvalid",
                        $"Input '{key}' has a filter with empty property.");
                }

                if (DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(filter.Property))
                {
                    return ProtectedPropertyError($"Input '{key}' filter references a protected property.");
                }

                if (!string.Equals(filter.Operator, "Equals", StringComparison.OrdinalIgnoreCase))
                {
                    return ErrorDetails.Validation(
                        "DocGen.UnsupportedInputFilterOperator",
                        $"Input '{key}' has unsupported filter operator '{filter.Operator}'.");
                }

                if (!allInputs.ContainsKey(filter.Input))
                {
                    return ErrorDetails.Validation(
                        "DocGen.InputFilterDependencyMissing",
                        $"Input '{key}' filter depends on unknown input '{filter.Input}'.");
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateMapping(MappingConfig mapping)
    {
        if (mapping.Scalars is not null)
        {
            foreach (var (tag, path) in mapping.Scalars)
            {
                if (DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(path))
                {
                    return ProtectedPropertyError($"Scalar mapping '{tag}' references a protected property.");
                }
            }
        }

        if (mapping.Tables is not null)
        {
            foreach (var (tableName, table) in mapping.Tables)
            {
                if (DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(table.SourceArray))
                {
                    return ProtectedPropertyError($"Table mapping '{tableName}' source references a protected property.");
                }

                foreach (var (columnTag, path) in table.RowMapping)
                {
                    if (DocumentGenerationAllowedEntities.ContainsProtectedPropertyReference(path))
                    {
                        return ProtectedPropertyError($"Table mapping '{tableName}.{columnTag}' references a protected property.");
                    }
                }
            }
        }

        return Result.Success();
    }

    private static bool IsSupportedValueType(string valueType)
    {
        return valueType is
            InputValueTypes.String or
            InputValueTypes.Int or
            InputValueTypes.Long or
            InputValueTypes.Guid or
            InputValueTypes.Bool or
            InputValueTypes.Date or
            InputValueTypes.DateTime or
            InputValueTypes.Decimal;
    }

    private static bool ShouldPreserveRawValue(string? valueType)
    {
        return valueType is InputValueTypes.String or InputValueTypes.Date or InputValueTypes.DateTime or null or "";
    }

    private static ErrorDetails InvalidInputType(string key, string expectedType)
    {
        return ErrorDetails.Validation(
            "DocGen.InvalidInputType",
            $"Input '{key}' must be a valid {expectedType} value.");
    }

    private static ErrorDetails ProtectedPropertyError(string message)
    {
        return ErrorDetails.Forbidden(
            "DocGen.ProtectedProperty",
            message);
    }
}
