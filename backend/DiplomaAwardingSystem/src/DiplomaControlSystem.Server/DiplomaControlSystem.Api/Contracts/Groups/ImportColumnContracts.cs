namespace DiplomaControlSystem.Api.Contracts.Groups;

public static class ImportColumnContracts
{
    public sealed record ImportColumnsResponse(IReadOnlyCollection<ImportColumnDto> Columns);

    public sealed record ImportColumnDto(
        string Key,
        string DisplayName,
        bool Required,
        IReadOnlyCollection<string> AcceptedHeaders);
}
