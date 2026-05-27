namespace DiplomaControlSystem.Api.Infrastructure.DefenceResultImports;

internal sealed record DefenceResultImportRow(
    string FullName,
    string SupervisorShortName,
    float? PlagiarismPercent,
    int? CommissionScore,
    int? SupervisorScore,
    int? ReviewerScore,
    string DefenceDate);
