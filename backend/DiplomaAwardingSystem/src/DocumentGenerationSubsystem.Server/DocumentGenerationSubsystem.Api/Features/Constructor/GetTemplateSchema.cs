using System.Text.Json.Serialization;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
    
#pragma warning disable SA1402

namespace DocumentGenerationSubsystem.Api.Features.Constructor;

internal static class GetTemplateSchema
{
    internal sealed record EntitySchemaNode(
        [property: JsonPropertyName("scalars")] IReadOnlyCollection<string> Scalars,
        [property: JsonPropertyName("entities")] IReadOnlyDictionary<string, string> Entities,
        [property: JsonPropertyName("collections")] IReadOnlyDictionary<string, string> Collections);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/documents/constructor/schema", Handle)
                .WithSummary("Gets the allowed database schema for the template constructor")
                .Produces<Ok<IReadOnlyDictionary<string, EntitySchemaNode>>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithTags("TemplateConstructor");
        }

        private static async Task<Results<Ok<IReadOnlyDictionary<string, EntitySchemaNode>>, ProblemHttpResult>> Handle(
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    internal sealed class Handler(IEntitySchemaProvider schemaProvider) : IScopedService
    {
        public Task<Result<IReadOnlyDictionary<string, EntitySchemaNode>>> HandleAsync(
            CancellationToken cancellationToken)
        {
            var schema = schemaProvider.GetSchema();

            return Task.FromResult(Result.Success(schema));
        }
    }
}

internal interface IEntitySchemaProvider : ISingletonService
{
    IReadOnlyDictionary<string, GetTemplateSchema.EntitySchemaNode> GetSchema();
}

internal sealed class EntitySchemaProvider : IEntitySchemaProvider
{
    private readonly IReadOnlyDictionary<string, GetTemplateSchema.EntitySchemaNode> _schemaCache;

    public EntitySchemaProvider(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DbDocGenContext>();

        _schemaCache = BuildSchemaCache(context);
    }

    public IReadOnlyDictionary<string, GetTemplateSchema.EntitySchemaNode> GetSchema() => _schemaCache;

    private static Dictionary<string, GetTemplateSchema.EntitySchemaNode> BuildSchemaCache(DbDocGenContext context)
    {
        var result = new Dictionary<string, GetTemplateSchema.EntitySchemaNode>(StringComparer.OrdinalIgnoreCase);
        var efModel = context.Model;

        foreach (var entityName in DocumentGenerationAllowedEntities.Registry.Keys)
        {
            var entityType = efModel.GetEntityTypes()
                .FirstOrDefault(e => e.ClrType.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

            if (entityType is null) continue;

            var scalars = entityType.GetProperties()
                .Where(p => !p.IsShadowProperty() && !p.IsForeignKey())
                .Select(p => p.Name)
                .ToArray();

            var entities = new Dictionary<string, string>();
            var collections = new Dictionary<string, string>();

            foreach (var nav in entityType.GetNavigations())
            {
                var targetTypeName = nav.TargetEntityType.ClrType.Name;
                if (!DocumentGenerationAllowedEntities.Registry.ContainsKey(targetTypeName)) continue;

                if (nav.IsCollection)
                    collections[nav.Name] = targetTypeName;
                else
                    entities[nav.Name] = targetTypeName;
            }

            result[entityName] = new GetTemplateSchema.EntitySchemaNode(scalars, entities, collections);
        }

        return result;
    }
}
