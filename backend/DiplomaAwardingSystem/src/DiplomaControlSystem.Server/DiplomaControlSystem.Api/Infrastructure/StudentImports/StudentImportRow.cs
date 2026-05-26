namespace DiplomaControlSystem.Api.Infrastructure.StudentImports;

internal sealed record StudentImportRow(
    string FullName,
    string SupervisorShortName,
    string Topic,
    string PracticeBase);
