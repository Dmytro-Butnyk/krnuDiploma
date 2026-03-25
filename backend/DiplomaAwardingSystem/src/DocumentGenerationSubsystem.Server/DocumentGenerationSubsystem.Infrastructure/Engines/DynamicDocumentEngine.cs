using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using DocumentGenerationSubsystem.Application.Interfaces;
using DocumentGenerationSubsystem.Domain.DependencyInjectionInterfaces;
using DocumentGenerationSubsystem.Domain.Entities.DocumentGeneration;
using FastMember;
using Microsoft.IO;
using Microsoft.EntityFrameworkCore;
using MiniSoftware;

namespace DocumentGenerationSubsystem.Infrastructure.Engines;

public sealed class DynamicDocumentEngine(DbDocGenContext dbContext) 
    : IDocumentGeneratorEngine, IScopedService
{
    // Пул потоков для предотвращения фрагментации Large Object Heap (LOH)
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
    
    // Кэш аксессоров типов для FastMember, чтобы не создавать их на каждый запрос
    private static readonly ConcurrentDictionary<Type, TypeAccessor> TypeAccessors = new();
    
    private static Dictionary<string, object> MapToMiniWordDictionary(MappingConfig mapping, Dictionary<string, object> dataContext)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // 1. Обработка скаляров
        if (mapping.Scalars != null)
        {
            foreach (var (wordTag, fullPath) in mapping.Scalars)
            {
                result[wordTag] = ExtractValueFromContext(dataContext, fullPath) ?? string.Empty;
            }
        }

        // 2. Обработка таблиц
        if (mapping.Tables != null)
        {
            foreach (var (tableName, tableConfig) in mapping.Tables)
            {
                var listResult = new List<Dictionary<string, object>>();
        
                var collectionObj = ExtractValueFromContext(dataContext, tableConfig.SourceArray);

                // УМНЫЙ ФОЛБЭК: Определяем, это реальная коллекция или одиночный объект
                IEnumerable<object>? collection = null;

                if (collectionObj is IEnumerable<object> enumerableObj && collectionObj is not string)
                {
                    // Это реальный список (например, MainGroup.Students)
                    collection = enumerableObj;
                }
                else if (collectionObj != null)
                {
                    // Это одиночный объект (например, TargetStudent).
                    // Оборачиваем его в массив из 1 элемента, чтобы удовлетворить MiniWord и Word-таблицу.
                    collection = new[] { collectionObj };
                }

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
                            // Жестко приводим к строке, чтобы MiniWord не падал на вложенных структурах
                            var rawValue = TraverseObjectGraph(item, columnPath);
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

    public async Task<Stream> GenerateAsync(
        string configurationJson, 
        byte[] wordTemplate, 
        IReadOnlyDictionary<string, string> parameters, 
        CancellationToken cancellationToken)
    {
        var config = JsonSerializer.Deserialize<TemplateConfiguration>(configurationJson)
            ?? throw new InvalidOperationException("Failed to parse template configuration.");

        // Словарь для хранения материализованных корней агрегатов (например, "TargetStudent" -> Student)
        var dataContext = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in config.DataSources)
        {
            var sourceData = await FetchDataAsync(source, parameters, cancellationToken);
            if (sourceData != null)
            {
                dataContext[source.Key] = sourceData;
            }
        }

        // Маппинг данных из графа объектов EF Core напрямую в словарь MiniWord
        var miniWordData = MapToMiniWordDictionary(config.Mapping, dataContext);

        // Используем переиспользуемый MemoryStream вместо выделения нового массива байтов
        var memoryStream = MemoryStreamManager.GetStream();
        await memoryStream.SaveAsByTemplateAsync(wordTemplate, miniWordData, cancellationToken);
        
        memoryStream.Position = 0;
        return memoryStream;
    }
    
    private static object? ExtractValueFromContext(Dictionary<string, object> dataContext, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return null;

        var dotIndex = fullPath.IndexOf('.', StringComparison.Ordinal);
        
        // Если пути нет (например, просто "TargetStudent"), возвращаем сам корень
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

                // Извлекаем имя свойства без аллокаций
                var propName = span[start..i].ToString();
                
                // Получаем кэшированный аксессор для типа
                var accessor = TypeAccessors.GetOrAdd(current.GetType(), TypeAccessor.Create);
                
                try
                {
                    current = accessor[current, propName];
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Свойство не найдено - мягко падаем (можно логировать)
                    return null;
                }

                start = i + 1;
            }
        }

        return current;
    }
    
    private static object?[] ParseFilterArguments(DataSourceConfig source, IReadOnlyDictionary<string, string> parameters)
    {
        var filterArgsList = source.FilterArgs as IList<string> ?? source.FilterArgs.ToList();
        var args = new object?[source.FilterArgs.Count];
        
        for (int i = 0; i < filterArgsList.Count; i++)
        {
            var argName = filterArgsList[i];
            if (!parameters.TryGetValue(argName, out var stringValue))
            {
                args[i] = null;
                continue;
            }

            // Эвристика конвертации типов (в реальном проекте типы лучше брать из метаданных)
            if (Guid.TryParse(stringValue, out var guidVal))
                args[i] = guidVal;
            else if (int.TryParse(stringValue, out var intVal))
                args[i] = intVal;
            else if (long.TryParse(stringValue, out var longVal))
                args[i] = longVal;
            else if (bool.TryParse(stringValue, out var boolVal))
                args[i] = boolVal;
            else
                args[i] = stringValue; // fallback to string
        }

        return args;
    }
    
    private static IQueryable<TEntity> BuildQuery<TEntity>(
        IQueryable<TEntity> dbSet, 
        IReadOnlyCollection<string>? includes)
        where TEntity : class
    {
        // Применяем оптимизации до приведения типов
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
    
    private async Task<object?> FetchDataAsync(
        DataSourceConfig source, 
        IReadOnlyDictionary<string, string> parameters, 
        CancellationToken cancellationToken)
    {
        // 1. Получаем динамический IQueryable с AsNoTracking и AsSplitQuery
        var query = GetDynamicQueryable(source);

        // 2. Применяем фильтрацию
        if (!string.IsNullOrWhiteSpace(source.Filter))
        {
            var args = ParseFilterArguments(source, parameters);
            query = query.Where(source.Filter, args);
        }

        // 3. ОГРАНИЧИВАЕМ выборку 1 элементом на стороне БД (LIMIT 1) и материализуем
        var resultList = await query.Take(1).ToDynamicListAsync(cancellationToken);
        
        return resultList.FirstOrDefault();
    }
    
    private IQueryable GetDynamicQueryable(DataSourceConfig source) => source.Entity switch
    {
        "Group" => BuildQuery(dbContext.Groups, source.Includes),
        "Rector" => BuildQuery(dbContext.Rectors, source.Includes),
        "Student" => BuildQuery(dbContext.Students, source.Includes),
        "Teacher" => BuildQuery(dbContext.Teachers, source.Includes),
        "QualificationWork" => BuildQuery(dbContext.QualificationWorks, source.Includes),
        _ => throw new UnauthorizedAccessException($"Security violation or unknown entity: '{source.Entity}'")
    };
}
