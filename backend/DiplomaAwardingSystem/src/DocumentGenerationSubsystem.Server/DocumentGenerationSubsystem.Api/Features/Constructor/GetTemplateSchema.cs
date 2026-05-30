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
        [property: JsonPropertyName("collections")] IReadOnlyDictionary<string, string> Collections,
        [property: JsonPropertyName("keyScalars")] IReadOnlyCollection<string> KeyScalars,
        [property: JsonPropertyName("foreignKeys")] IReadOnlyCollection<ForeignKeySchemaNode> ForeignKeys,
        [property: JsonPropertyName("references")] IReadOnlyCollection<EntityReferenceSchemaNode> References,
        [property: JsonPropertyName("displayCandidates")] IReadOnlyCollection<string> DisplayCandidates);

    internal sealed record ForeignKeySchemaNode(
        [property: JsonPropertyName("property")] string Property,
        [property: JsonPropertyName("targetEntity")] string TargetEntity);

    internal sealed record EntityReferenceSchemaNode(
        [property: JsonPropertyName("navigation")] string Navigation,
        [property: JsonPropertyName("targetEntity")] string TargetEntity,
        [property: JsonPropertyName("foreignKeys")] IReadOnlyCollection<string> ForeignKeys,
        [property: JsonPropertyName("isCollection")] bool IsCollection);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/constructor/schema", Handle)
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

    private sealed class Handler(IEntitySchemaProvider schemaProvider) : IScopedService
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

        foreach (var (entityName, registration) in DocumentGenerationAllowedEntities.Registry)
        {
            var entityType = efModel.GetEntityTypes()
                .FirstOrDefault(e => e.ClrType == registration.ClrType);

            if (entityType is null) continue;

            var scalars = entityType.GetProperties()
                .Where(p => !p.IsShadowProperty()
                            && !p.IsForeignKey()
                            && registration.AllowsProperty(p.Name))
                .Select(p => p.Name)
                .ToArray();

            var keyScalars = entityType.FindPrimaryKey()?.Properties
                .Where(p => !p.IsShadowProperty()
                            && registration.AllowsProperty(p.Name))
                .Select(p => p.Name)
                .ToArray()
                ?? [];

            var foreignKeys = entityType.GetForeignKeys()
                .Where(fk => DocumentGenerationAllowedEntities.Registry.ContainsKey(fk.PrincipalEntityType.ClrType.Name))
                .SelectMany(fk => fk.Properties
                    .Where(property => registration.AllowsProperty(property.Name))
                    .Select(property => new GetTemplateSchema.ForeignKeySchemaNode(
                        property.Name,
                        fk.PrincipalEntityType.ClrType.Name)))
                .ToArray();

            var entities = new Dictionary<string, string>();
            var collections = new Dictionary<string, string>();
            var references = new List<GetTemplateSchema.EntityReferenceSchemaNode>();

            foreach (var nav in entityType.GetNavigations())
            {
                var targetTypeName = nav.TargetEntityType.ClrType.Name;
                if (!DocumentGenerationAllowedEntities.Registry.ContainsKey(targetTypeName)) continue;

                if (nav.IsCollection)
                    collections[nav.Name] = targetTypeName;
                else
                    entities[nav.Name] = targetTypeName;

                references.Add(new GetTemplateSchema.EntityReferenceSchemaNode(
                    nav.Name,
                    targetTypeName,
                    nav.ForeignKey.Properties
                        .Where(p => registration.AllowsProperty(p.Name))
                        .Select(p => p.Name)
                        .ToArray(),
                    nav.IsCollection));
            }

            var displayCandidates = BuildDisplayCandidates(scalars);

            result[entityName] = new GetTemplateSchema.EntitySchemaNode(
                scalars,
                entities,
                collections,
                keyScalars,
                foreignKeys,
                references,
                displayCandidates);
        }

        return result;
    }

    private static string[] BuildDisplayCandidates(IReadOnlyCollection<string> scalars)
    {
        string[] preferred =
        [
            "FullName",
            "Name",
            "ShortName",
            "Code",
            "Email",
            "OrderNumber",
            "Year",
            "DefenseYear",
            "Topic"
        ];

        return preferred
            .Where(scalars.Contains)
            .Concat(scalars.Where(scalar => scalar.EndsWith("Name", StringComparison.Ordinal)
                                            && !preferred.Contains(scalar, StringComparer.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
