using System.Collections;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using DocumentGenerationSubsystem.Api.ErrorsAndLogs;
using DocumentGenerationSubsystem.Api.Infrastructure.Configuration;
using DocumentGenerationSubsystem.Api.Infrastructure.Security;
using FastMember;
using Microsoft.EntityFrameworkCore;
using Microsoft.IO;
using MiniSoftware;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Engines;

/// <summary>
/// Engine responsible for generating Word documents by fetching data via Dynamic LINQ 
/// and mapping it to templates using MiniWord.
/// </summary>
public sealed class DynamicDocumentEngine(
    DbDocGenContext dbContext,
    ILogger<DynamicDocumentEngine> logger)
    : IScopedService
{
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
    private static readonly ConcurrentDictionary<Type, TypeAccessor> TypeAccessors = new();

    /// <summary>
    /// Transforms the raw data context into a dictionary structure compatible with MiniWord,
    /// handling both scalar values and complex tables.
    /// </summary>
    /// <param name="mapping">The mapping configuration defining how data maps to Word tags.</param>
    /// <param name="dataContext">The source data fetched from the database.</param>
    private static Result<Dictionary<string, object>> MapToMiniWordDictionary(
        MappingConfig? mapping,
        Dictionary<string, object> dataContext)
    {
        if (mapping == null) return DynamicDocumentEngineErrors.InvalidConfiguration;

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // 1. Map Scalars
        if (mapping.Scalars != null)
        {
            foreach (var (wordTag, fullPath) in mapping.Scalars)
            {
                var val = ExtractValueFromContext(dataContext, fullPath);

                if (val is IEnumerable and not string)
                {
                    return DynamicDocumentEngineErrors.NestedListNotSupported;
                }

                result[wordTag] = val ?? string.Empty;
            }
        }

        // 2. Map Tables
        if (mapping.Tables != null)
        {
            foreach (var (tableName, tableConfig) in mapping.Tables)
            {
                if (tableConfig == null) continue;

                var collectionObj = ExtractValueFromContext(dataContext, tableConfig.SourceArray);

                // FIX: Bulletproof IEnumerable extraction to avoid covariance casting failures
                IEnumerable<object>? collection = collectionObj switch
                {
                    string str => [str], // Prevent string from being iterated as chars
                    IEnumerable enumerable => enumerable.Cast<object>(), // Safe box elements
                    not null => [collectionObj],
                    _ => null
                };

                var listResult = new List<Dictionary<string, object>>();

                if (collection != null)
                {
                    int index = 1;
                    foreach (var item in collection)
                    {
                        var rowDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["Number"] = index++
                        };

                        foreach (var (columnTag, columnPath) in tableConfig.RowMapping)
                        {
                            var rawValue = TraverseObjectGraph(item, columnPath);

                            if (rawValue is IEnumerable and not string)
                            {
                                return DynamicDocumentEngineErrors.NestedListNotSupported;
                            }

                            rowDict[columnTag] = rawValue?.ToString() ?? string.Empty;
                        }

                        listResult.Add(rowDict);
                    }
                }

                result[tableName] = listResult;
            }
        }

        return result;
    }

    /// <summary>
    /// Orchestrates the document generation process: parses config, fetches data, maps it, and renders the template.
    /// </summary>
    /// <param name="configurationJson">JSON string containing <see cref="TemplateConfiguration"/>.</param>
    /// <param name="wordTemplate">The .docx template file as a byte array.</param>
    /// <param name="parameters">External parameters (e.g., IDs) used for filtering data sources.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result<Stream>> GenerateAsync(
        string configurationJson,
        byte[] wordTemplate,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var configResult = TemplateConfigurationReader.Parse(configurationJson);
        if (configResult.IsFailure)
        {
            logger.LogMappingFailed(configResult.ErrorDetails.Code, configResult.ErrorDetails.Message);
            return configResult.ErrorDetails;
        }

        var config = configResult.Value!;
        logger.LogGenerationStarted(configurationJson.Length);

        var inputContextResult = TemplateConfigurationReader.BuildInputContext(config, parameters);
        if (inputContextResult.IsFailure)
        {
            return inputContextResult.ErrorDetails;
        }

        var entitySelectValidationResult = await ValidateEntitySelectInputsAsync(
            config,
            parameters,
            cancellationToken);
        if (entitySelectValidationResult.IsFailure)
        {
            return entitySelectValidationResult.ErrorDetails;
        }
        
        var dataContext = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Input"] = inputContextResult.Value!
        };

        // Fetch data via Dynamic LINQ
        if (config.DataSources != null)
        {
            foreach (var source in config.DataSources)
            {
                var fetchResult = await FetchDataAsync(source, parameters, cancellationToken);

                if (fetchResult.IsFailure)
                    return fetchResult.ErrorDetails;

                if (fetchResult.Value != null)
                {
                    dataContext[source.Key] = fetchResult.Value;
                }
            }
        }

        var mappingResult = MapToMiniWordDictionary(config.Mapping!, dataContext);
        if (mappingResult.IsFailure)
        {
            logger.LogMappingFailed(mappingResult.ErrorDetails.Code, mappingResult.ErrorDetails.Message);
            return mappingResult.ErrorDetails;
        }

        var memoryStream = MemoryStreamManager.GetStream();

        try
        {
            await memoryStream.SaveAsByTemplateAsync(wordTemplate, mappingResult.Value, cancellationToken);
            memoryStream.Position = 0;
            
            logger.LogGenerationCompleted(config.DataSources?.Count ?? 0);
            return memoryStream;
        }
        catch (Exception ex)
        {
            logger.LogMiniWordCrash(ex);
            await memoryStream.DisposeAsync();
            return DynamicDocumentEngineErrors.MiniWordGenerationFailed;
        }
    }

    /// <summary>
    /// Extracts a value from the data context using a dot-notated path (e.g., "Student.Group.Name").
    /// </summary>
    private static object? ExtractValueFromContext(Dictionary<string, object> dataContext, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return null;

        var dotIndex = fullPath.IndexOf('.', StringComparison.Ordinal);

        if (dotIndex == -1)
        {
            return dataContext.TryGetValue(fullPath, out var val) ? val : null;
        }

        // Using ranges for allocation reduction (although strings are still allocated, it's cleaner)
        var rootKey = fullPath[..dotIndex];
        var propPath = fullPath[(dotIndex + 1)..];

        if (!dataContext.TryGetValue(rootKey, out var rootObj) || rootObj == null)
            return null;

        return TraverseObjectGraph(rootObj, propPath);
    }

    /// <summary>
    /// Recursively traverses an object's properties using FastMember for high-performance reflection.
    /// </summary>
    /// <param name="obj">The root object or dictionary to read from.</param>
    /// <param name="path">The dot-notated property path.</param>
    /// <returns>The resolved value, or null when the path cannot be resolved.</returns>
    internal static object? TraverseObjectGraph(object? obj, string path)
    {
        if (obj == null || string.IsNullOrWhiteSpace(path)) return obj;

        object? current = obj;
        var span = path.AsSpan();
        int start = 0;

        for (int i = 0; i <= span.Length; i++)
        {
            if (i == span.Length || span[i] == '.')
            {
                if (current == null) return null;

                var propName = span[start..i].ToString();

                if (current is IReadOnlyDictionary<string, object?> readOnlyDictionary)
                {
                    current = readOnlyDictionary.TryGetValue(propName, out var dictionaryValue)
                        ? dictionaryValue
                        : null;
                    start = i + 1;
                    continue;
                }

                if (current is IDictionary<string, object?> dictionary)
                {
                    current = dictionary.TryGetValue(propName, out var dictionaryValue)
                        ? dictionaryValue
                        : null;
                    start = i + 1;
                    continue;
                }

                var accessor = TypeAccessors.GetOrAdd(current.GetType(), TypeAccessor.Create);

                try
                {
                    current = accessor[current, propName];
                }
                catch (ArgumentOutOfRangeException)
                {
                    // FastMember throws this if the property doesn't exist on the type.
                    return null;
                }

                start = i + 1;
            }
        }

        return current;
    }

    /// <summary>
    /// Parses string parameters into their strongly typed equivalents for Dynamic LINQ consumption.
    /// </summary>
    private static object?[] ParseFilterArguments(
        DataSourceConfig source,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (source.FilterArgs == null || source.FilterArgs.Count == 0)
            return [];

        // Optimize to avoid allocations if it's already an array/list
        var filterArgsList = source.FilterArgs as IList<string> ?? source.FilterArgs.ToList();
        var args = new object?[filterArgsList.Count];

        for (int i = 0; i < filterArgsList.Count; i++)
        {
            var argName = filterArgsList[i];
            
            if (parameters == null || !parameters.TryGetValue(argName, out var stringValue))
            {
                args[i] = null;
                continue;
            }

            // Simple fast-path type parsing
            if (Guid.TryParse(stringValue, out var guidVal))
                args[i] = guidVal;
            else if (int.TryParse(stringValue, out var intVal))
                args[i] = intVal;
            else if (long.TryParse(stringValue, out var longVal))
                args[i] = longVal;
            else if (bool.TryParse(stringValue, out var boolVal))
                args[i] = boolVal;
            else
                args[i] = stringValue;
        }

        return args;
    }

    /// <summary>
    /// Configures an IQueryable with standard tracking and inclusion settings.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity in the query.</typeparam>
    /// <param name="dbSet">The source <see cref="IQueryable{T}"/> to configure.</param>
    /// <param name="includes">A collection of navigation properties to include in the query.</param>
    /// <returns>An <see cref="IQueryable{TEntity}"/> with tracking disabled and includes applied.</returns>
    internal static IQueryable<TEntity> BuildQuery<TEntity>(
        IQueryable<TEntity> dbSet,
        IReadOnlyCollection<string>? includes)
        where TEntity : class
    {
        var query = dbSet.AsNoTracking().AsSplitQuery();

        if (includes != null && includes.Count > 0)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        return query;
    }

    /// <summary>
    /// Executes a dynamic query against the database based on the data source configuration.
    /// </summary>
    private async Task<Result<object?>> FetchDataAsync(
        DataSourceConfig source,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var queryResult = GetDynamicQueryable(source);
        if (queryResult.IsFailure)
            return queryResult.ErrorDetails;

        var query = queryResult.Value!;

        try
        {
            if (!string.IsNullOrWhiteSpace(source.Filter))
            {
                var args = ParseFilterArguments(source, parameters);
                query = query.Where(source.Filter, args);
            }

            // Optimization: Grab exactly one via dynamic LINQ
            var resultList = await query.Take(1).ToDynamicListAsync(cancellationToken);
            return resultList.FirstOrDefault();
        }
        catch (ParseException ex)
        {
            logger.LogDynamicLinqError(ex, source.Entity);
            return DynamicDocumentEngineErrors.DynamicLinqError;
        }
        catch (DbException ex)
        {
            logger.LogDatabaseError(ex, source.Entity);
            return DynamicDocumentEngineErrors.DatabaseError;
        }
    }

    /// <summary>
    /// Resolves the base IQueryable for a specific entity type from the security registry.
    /// </summary>
    private Result<IQueryable> GetDynamicQueryable(DataSourceConfig source)
    {
        if (DocumentGenerationAllowedEntities.Registry.TryGetValue(source.Entity, out var registration))
        {
            return Result.Success(registration.QueryFactory(dbContext, source.Includes));
        }

        logger.LogUnauthorizedEntity(source.Entity);
        return DynamicDocumentEngineErrors.UnauthorizedEntity;
    }

    private async Task<Result> ValidateEntitySelectInputsAsync(
        TemplateConfiguration configuration,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        if (configuration.Inputs is null || configuration.Inputs.Count == 0)
        {
            return Result.Success();
        }

        foreach (var (key, input) in configuration.Inputs)
        {
            if (!string.Equals(input.Kind, InputKinds.EntitySelect, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!parameters.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.Entity)
                || !DocumentGenerationAllowedEntities.Registry.TryGetValue(input.Entity, out var registration))
            {
                return DynamicDocumentEngineErrors.UnauthorizedEntity;
            }

            var parsedValueResult = TemplateConfigurationReader.ParseInputValue(key, input.ValueType, rawValue);
            if (parsedValueResult.IsFailure)
            {
                return parsedValueResult.ErrorDetails;
            }

            IQueryable query = registration.QueryFactory(dbContext, null);
            query = query.Where("Id == @0", parsedValueResult.Value);

            var filtersResult = ApplyEntitySelectFilters(query, key, input, configuration.Inputs, parameters);
            if (filtersResult.IsFailure)
            {
                return filtersResult.ErrorDetails;
            }

            query = filtersResult.Value!;

            try
            {
                var matches = await query.Take(1).ToDynamicListAsync(cancellationToken);
                if (matches.Count == 0)
                {
                    return ErrorDetails.Validation(
                        "DocGen.InvalidEntitySelection",
                        $"Selected value for input '{key}' was not found or does not match its filters.");
                }
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

        return Result.Success();
    }

    internal static Result<IQueryable> ApplyEntitySelectFilters(
        IQueryable query,
        string key,
        InputConfig input,
        IReadOnlyDictionary<string, InputConfig> allInputs,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (input.Filters is null || input.Filters.Count == 0)
        {
            return Result.Success(query);
        }

        foreach (var filter in input.Filters)
        {
            if (!allInputs.TryGetValue(filter.Input, out var dependencyInput)
                || !parameters.TryGetValue(filter.Input, out var dependencyRawValue)
                || string.IsNullOrWhiteSpace(dependencyRawValue))
            {
                return ErrorDetails.Validation(
                    "DocGen.MissingInputDependency",
                    $"Input '{key}' requires selected dependency '{filter.Input}'.");
            }

            var dependencyValueResult = TemplateConfigurationReader.ParseInputValue(
                filter.Input,
                dependencyInput.ValueType,
                dependencyRawValue);

            if (dependencyValueResult.IsFailure)
            {
                return dependencyValueResult.ErrorDetails;
            }

            query = query.Where($"{filter.Property} == @0", dependencyValueResult.Value);
        }

        return Result.Success(query);
    }
}
