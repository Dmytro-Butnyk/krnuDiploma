using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Text.Json;
using Core.Domain.ResultPattern;
using DocumentGenerationSubsystem.Application.Interfaces;
using DocumentGenerationSubsystem.Domain.DependencyInjectionInterfaces;
using DocumentGenerationSubsystem.Domain.Entities.DocumentGeneration;
using DocumentGenerationSubsystem.Domain.Entities.ErrorDetailsDescriptions;
using FastMember;
using Microsoft.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniSoftware;

namespace DocumentGenerationSubsystem.Infrastructure.Engines;

public sealed class DynamicDocumentEngine(
    DbDocGenContext dbContext,
    ILogger<DynamicDocumentEngine> logger)
    : IDocumentGeneratorEngine, IScopedService
{
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
    private static readonly ConcurrentDictionary<Type, TypeAccessor> TypeAccessors = new();

    private static Result<Dictionary<string, object>> MapToMiniWordDictionary(
        MappingConfig mapping,
        Dictionary<string, object> dataContext)
    {
        if (mapping == null) return DocumentErrors.InvalidConfiguration;

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (mapping.Scalars != null)
        {
            foreach (var (wordTag, fullPath) in mapping.Scalars)
            {
                var val = ExtractValueFromContext(dataContext, fullPath);

                if (val is System.Collections.IEnumerable and not string)
                {
                    return DocumentErrors.NestedListNotSupported;
                }

                result[wordTag] = val ?? string.Empty;
            }
        }

        if (mapping.Tables != null)
        {
            foreach (var (tableName, tableConfig) in mapping.Tables)
            {
                if (tableConfig == null) continue;

                var listResult = new List<Dictionary<string, object>>();
                var collectionObj = ExtractValueFromContext(dataContext, tableConfig.SourceArray);

                IEnumerable<object>? collection = collectionObj switch
                {
                    IEnumerable<object> enumerableObj => enumerableObj,
                    not null => [collectionObj],
                    _ => null
                };

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

                            if (rawValue is System.Collections.IEnumerable and not string)
                            {
                                return DocumentErrors.NestedListNotSupported;
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

    public async Task<Result<Stream>> GenerateAsync(
        string configurationJson,
        byte[] wordTemplate,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        TemplateConfiguration? config;
        try
        {
            config = JsonSerializer.Deserialize<TemplateConfiguration>(configurationJson);
            if (config == null) return DocumentErrors.InvalidConfiguration;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize template configuration");
            return DocumentErrors.InvalidConfiguration;
        }

        if (config.DataSources != null)
        {
            foreach (var source in config.DataSources)
            {
                if (source.FilterArgs == null || source.FilterArgs.Count == 0) 
                    continue;

                foreach (var requiredArg in source.FilterArgs)
                {
                    if (parameters == null || !parameters.TryGetValue(requiredArg, out var val) || string.IsNullOrWhiteSpace(val))
                    {
                        logger.LogWarning("Missing or empty required parameter '{Parameter}' for data source '{SourceKey}'", requiredArg, source.Key);
                        return DocumentErrors.MissingParameter(requiredArg, source.Key);
                    }
                }
            }
        }
        
        var dataContext = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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
            return mappingResult.ErrorDetails;

        var memoryStream = MemoryStreamManager.GetStream();

        try
        {
            await memoryStream.SaveAsByTemplateAsync(wordTemplate, mappingResult.Value, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MiniWord engine crashed during document generation");
            await memoryStream.DisposeAsync();
            return DocumentErrors.MiniWordGenerationFailed;
        }
    }

    private static object? ExtractValueFromContext(Dictionary<string, object> dataContext, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return null;

        var dotIndex = fullPath.IndexOf('.', StringComparison.Ordinal);

        if (dotIndex == -1)
        {
            return dataContext.TryGetValue(fullPath, out var val) ? val : null;
        }

        var rootKey = fullPath[..dotIndex];
        var propPath = fullPath[(dotIndex + 1)..];

        if (!dataContext.TryGetValue(rootKey, out var rootObj) || rootObj == null)
            return null;

        return TraverseObjectGraph(rootObj, propPath);
    }

    private static object? TraverseObjectGraph(object? obj, string path)
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

                var accessor = TypeAccessors.GetOrAdd(current.GetType(), TypeAccessor.Create);

                try
                {
                    current = accessor[current, propName];
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }

                start = i + 1;
            }
        }

        return current;
    }

    private static object?[] ParseFilterArguments(
        DataSourceConfig source,
        IReadOnlyDictionary<string, string> parameters)
    {
        var filterArgsList = source.FilterArgs as IList<string> ?? source.FilterArgs!.ToList();
        var args = new object?[source.FilterArgs!.Count];

        for (int i = 0; i < filterArgsList.Count; i++)
        {
            var argName = filterArgsList[i];
            
            if (parameters == null || !parameters.TryGetValue(argName, out var stringValue))
            {
                args[i] = null;
                continue;
            }

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

    private static IQueryable<TEntity> BuildQuery<TEntity>(
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

            var resultList = await query.Take(1).ToDynamicListAsync(cancellationToken);
            return resultList.FirstOrDefault();
        }
        catch (ParseException ex)
        {
            logger.LogWarning(ex, "Invalid dynamic LINQ syntax for entity {Entity}", source.Entity);
            return DocumentErrors.DynamicLinqError;
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error during data fetching for entity {Entity}", source.Entity);
            return DocumentErrors.DatabaseError;
        }
    }

    private Result<IQueryable> GetDynamicQueryable(DataSourceConfig source) => source.Entity switch
    {
        // Archive group
        "Archive" => Result.Success((IQueryable)BuildQuery(dbContext.Archives, source.Includes)),
        "Defence" => Result.Success((IQueryable)BuildQuery(dbContext.Defences, source.Includes)),
        "QualificationWork" => Result.Success((IQueryable)BuildQuery(dbContext.QualificationWorks, source.Includes)),
        
        // Study group
        "Department" => Result.Success((IQueryable)BuildQuery(dbContext.Departments, source.Includes)),
        "Group" => Result.Success((IQueryable)BuildQuery(dbContext.Groups, source.Includes)),
        "Specialty" => Result.Success((IQueryable)BuildQuery(dbContext.Specialties, source.Includes)),
        "Student" => Result.Success((IQueryable)BuildQuery(dbContext.Students, source.Includes)),
        
        // Teacher staff
        "AcademicDegree" => Result.Success((IQueryable)BuildQuery(dbContext.AcademicDegrees, source.Includes)),
        "DecMember" => Result.Success((IQueryable)BuildQuery(dbContext.DecMembers, source.Includes)),
        "DecToMember" => Result.Success((IQueryable)BuildQuery(dbContext.DecToMembers, source.Includes)),
        "DiplomaExaminationCommission" => Result.Success((IQueryable)BuildQuery(dbContext.DiplomaExaminationCommissions, source.Includes)),
        "Teacher" => Result.Success((IQueryable)BuildQuery(dbContext.Teachers, source.Includes)),
        
        _ => DocumentErrors.UnauthorizedEntity
    };
}
