using System.Data.Common;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using DocumentGenerationSubsystem.Api.ErrorsAndLogs;
using DocumentGenerationSubsystem.Api.Infrastructure.Configuration;
using DocumentGenerationSubsystem.Api.Infrastructure.Engines;
using DocumentGenerationSubsystem.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class GetGenerationInputOptions
{
    public sealed record OptionDto(string Value, string Label, string? Description);

    public sealed record OptionsResponse(IReadOnlyCollection<OptionDto> Items, bool HasMore);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/documents/templates/{templateId:int}/generation-inputs/{inputKey}/options", Handle)
                .WithSummary("Gets lazy-loaded options for an entity-select generation input")
                .Produces<OptionsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<Ok<OptionsResponse>, ProblemHttpResult>> Handle(
            [FromRoute] int templateId,
            [FromRoute] string inputKey,
            HttpContext httpContext,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var request = OptionsRequest.FromQuery(httpContext.Request.Query);
            var result = await handler.HandleAsync(templateId, inputKey, request, ct);

            return result.IsFailure
                ? result.ToProblemDetails()
                : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        ILogger<Handler> logger) : IScopedService
    {
        private const int DefaultTake = 30;
        private const int MaxTake = 100;

        public async Task<Result<OptionsResponse>> HandleAsync(
            int templateId,
            string inputKey,
            OptionsRequest request,
            CancellationToken ct)
        {
            var template = await context.Set<DocumentTemplate>()
                .AsNoTracking()
                .Where(t => t.Id == templateId)
                .Select(t => new { t.ConfigurationJson })
                .FirstOrDefaultAsync(ct);

            if (template is null)
            {
                return ErrorDetails.NotFound("Template.NotFound", "Template not found.");
            }

            var configResult = TemplateConfigurationReader.Parse(template.ConfigurationJson);
            if (configResult.IsFailure)
            {
                return ErrorDetails.Conflict(
                    "Template.CorruptedData",
                    "Template configuration is invalid or corrupted.");
            }

            var configuration = configResult.Value!;
            if (configuration.Inputs is null || !configuration.Inputs.TryGetValue(inputKey, out var input))
            {
                return ErrorDetails.NotFound(
                    "DocGen.InputNotFound",
                    $"Generation input '{inputKey}' was not found.");
            }

            var isEntitySelect = string.Equals(input.Kind, InputKinds.EntitySelect, StringComparison.OrdinalIgnoreCase);
            var isValueSelect = string.Equals(input.Kind, InputKinds.ValueSelect, StringComparison.OrdinalIgnoreCase);

            if (!isEntitySelect && !isValueSelect)
            {
                return ErrorDetails.Validation(
                    "DocGen.InputOptionsNotSupported",
                    $"Generation input '{inputKey}' does not support options.");
            }

            if (string.IsNullOrWhiteSpace(input.Entity)
                || !DocumentGenerationAllowedEntities.Registry.TryGetValue(input.Entity, out var registration))
            {
                return DynamicDocumentEngineErrors.UnauthorizedEntity;
            }

            IQueryable query = registration.QueryFactory(context, null);

            var filtersResult = DynamicDocumentEngine.ApplyEntitySelectFilters(
                query,
                inputKey,
                input,
                configuration.Inputs,
                request.Parameters);

            if (filtersResult.IsFailure)
            {
                return filtersResult.ErrorDetails;
            }

            query = filtersResult.Value!;

            try
            {
                var take = Math.Clamp(request.Take ?? DefaultTake, 1, MaxTake);
                OptionDto[] options;
                bool hasMore;

                if (isValueSelect)
                {
                    var valueOptions = await LoadValueSelectOptionsAsync(query, input, take, ct);
                    options = valueOptions.Options;
                    hasMore = valueOptions.HasMore;
                }
                else
                {
                    query = ApplySearch(query, input, request.Search);
                    query = ApplyOrderBy(query, input);

                    var rows = await query.Take(take + 1).ToDynamicListAsync(ct);
                    hasMore = rows.Count > take;

                    options = rows.Cast<object>()
                        .Take(take)
                        .Select(row => MapOption(row, input))
                        .ToArray();
                }

                return new OptionsResponse(options, hasMore);
            }
            catch (ParseException ex)
            {
                logger.LogDynamicLinqError(ex, input.Entity);
                return DynamicDocumentEngineErrors.DynamicLinqError;
            }
            catch (DbException ex)
            {
                logger.LogDatabaseError(ex, input.Entity);
                return DynamicDocumentEngineErrors.DatabaseError;
            }
        }

        private static IQueryable ApplySearch(IQueryable query, InputConfig input, string? search)
        {
            if (string.IsNullOrWhiteSpace(search) || input.Search is null || input.Search.Count == 0)
            {
                return query;
            }

            var terms = input.Search
                .Where(field => !string.IsNullOrWhiteSpace(field))
                .Select(field => $"{field} != null && {field}.Contains(@0)")
                .ToArray();

            return terms.Length == 0
                ? query
                : query.Where(string.Join(" || ", terms), search.Trim());
        }

        private static IQueryable ApplyOrderBy(IQueryable query, InputConfig input)
        {
            if (input.OrderBy is null || input.OrderBy.Count == 0)
            {
                return string.Equals(input.Kind, InputKinds.ValueSelect, StringComparison.OrdinalIgnoreCase)
                       && !string.IsNullOrWhiteSpace(input.ValuePath)
                    ? query.OrderBy(input.ValuePath)
                    : query.OrderBy("Id");
            }

            var orderBy = string.Join(", ", input.OrderBy.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.IsNullOrWhiteSpace(orderBy)
                ? query.OrderBy("Id")
                : query.OrderBy(orderBy);
        }

        private static async Task<(OptionDto[] Options, bool HasMore)> LoadValueSelectOptionsAsync(
            IQueryable query,
            InputConfig input,
            int take,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(input.ValuePath))
            {
                return ([], false);
            }

            query = query.Where($"{input.ValuePath} != null");
            query = ApplyOrderBy(query, input);

            var rows = await query.Take(1000).ToDynamicListAsync(ct);
            var options = rows.Cast<object>()
                .Select(row => DynamicDocumentEngine.TraverseObjectGraph(row, input.ValuePath))
                .Where(value => value is not null)
                .Distinct()
                .Select(value => MapValueOption(value!, input))
                .Take(take + 1)
                .ToArray();

            return (options.Take(take).ToArray(), options.Length > take);
        }

        private static OptionDto MapOption(object row, InputConfig input)
        {
            var value = DynamicDocumentEngine.TraverseObjectGraph(row, "Id")?.ToString() ?? string.Empty;
            var label = BuildText(row, input.Display);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = BuildText(row, ["FullName", "Name", "ShortName", "Code"]);
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                label = value;
            }

            var description = BuildText(row, input.Description);
            return new OptionDto(value, label, string.IsNullOrWhiteSpace(description) ? null : description);
        }

        private static OptionDto MapValueOption(object value, InputConfig input)
        {
            return value switch
            {
                DateOnly date => new OptionDto(
                    date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    null),
                DateTime dateTime => new OptionDto(
                    dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    dateTime.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    null),
                _ => new OptionDto(value.ToString() ?? string.Empty, value.ToString() ?? string.Empty, null)
            };
        }

        private static string BuildText(object row, IReadOnlyCollection<string>? fields)
        {
            if (fields is null || fields.Count == 0)
            {
                return string.Empty;
            }

            var parts = fields
                .Select(field => DynamicDocumentEngine.TraverseObjectGraph(row, field)?.ToString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            return string.Join(" - ", parts);
        }
    }

    private sealed record OptionsRequest(
        string? Search,
        int? Take,
        IReadOnlyDictionary<string, string> Parameters)
    {
        public static OptionsRequest FromQuery(IQueryCollection query)
        {
            query.TryGetValue("q", out var q);
            int? take = null;

            if (query.TryGetValue("take", out var takeValue)
                && int.TryParse(takeValue.ToString(), out var parsedTake))
            {
                take = parsedTake;
            }

            var parameters = query
                .Where(pair => !string.Equals(pair.Key, "q", StringComparison.OrdinalIgnoreCase)
                               && !string.Equals(pair.Key, "take", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);

            return new OptionsRequest(q.ToString(), take, parameters);
        }
    }
}
