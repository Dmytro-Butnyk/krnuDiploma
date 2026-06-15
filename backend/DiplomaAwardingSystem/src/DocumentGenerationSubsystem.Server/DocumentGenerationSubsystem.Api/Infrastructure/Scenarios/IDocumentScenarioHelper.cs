using Core.Domain.ResultPattern;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Scenarios;

public interface IDocumentScenarioHelper
{
    string Key { get; }

    Task<Result<IReadOnlyDictionary<string, object>>> BuildAsync(
        DocumentScenarioContext context,
        CancellationToken ct);
}
