namespace DiplomaControlSystem.Api.Infrastructure.ImportColumns;

internal sealed record ImportColumnDefinition(
    string Key,
    string DisplayName,
    bool Required,
    IReadOnlyCollection<string> AcceptedHeaders);
