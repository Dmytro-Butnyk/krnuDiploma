namespace DiplomaControlSystem.Api.Infrastructure.DefenceResultImports;

internal sealed record DefenceResultImportRow(
    string FullName,
    string SupervisorShortName,
    float? PlagiarismPercent,
    int? CommissionScore,
    string DefenceDate);
