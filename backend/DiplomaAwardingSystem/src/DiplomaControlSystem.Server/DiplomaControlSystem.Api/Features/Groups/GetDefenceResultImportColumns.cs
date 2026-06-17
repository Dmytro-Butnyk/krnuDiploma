using DiplomaControlSystem.Api.Infrastructure.DefenceResultImports;
using DiplomaControlSystem.Api.Infrastructure.ImportColumns;
using Microsoft.AspNetCore.Http.HttpResults;
using static DiplomaControlSystem.Api.Contracts.Groups.ImportColumnContracts;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetDefenceResultImportColumns
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/groups/defence-results/import/columns", Handle)
                .WithSummary("Gets accepted defence result import table columns")
                .Produces<ImportColumnsResponse>(StatusCodes.Status200OK)
                .WithTags("Groups");
        }

        private static Ok<ImportColumnsResponse> Handle()
        {
            return TypedResults.Ok(CreateResponse(DefenceResultImportColumnDefinitions.All));
        }

        private static ImportColumnsResponse CreateResponse(IReadOnlyCollection<ImportColumnDefinition> columns)
        {
            return new ImportColumnsResponse(
                columns.Select(column => new ImportColumnDto(
                    column.Key,
                    column.DisplayName,
                    column.Required,
                    column.AcceptedHeaders)).ToList());
        }
    }
}
