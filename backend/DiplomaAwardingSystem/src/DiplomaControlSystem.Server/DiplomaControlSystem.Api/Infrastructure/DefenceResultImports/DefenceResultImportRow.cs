namespace DiplomaControlSystem.Api.Infrastructure.DefenceResultImports;

internal sealed record DefenceResultImportRow(
    string FullName,
    float? PlagiarismPercent,
    int? CommissionScore,
    int? SupervisorScore,
    int? ReviewerScore,
    string DefenceDate);
