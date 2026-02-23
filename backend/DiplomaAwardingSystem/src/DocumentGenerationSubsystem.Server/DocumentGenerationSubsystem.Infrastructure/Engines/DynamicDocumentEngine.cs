using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocumentGenerationSubsystem.Application.Interfaces;
using DocumentGenerationSubsystem.Domain.DependencyInjectionInterfaces;
using DocumentGenerationSubsystem.Domain.Entities.DocumentGeneration;
using Microsoft.EntityFrameworkCore;
using MiniSoftware;

namespace DocumentGenerationSubsystem.Infrastructure.Engines;

public sealed class DynamicDocumentEngine(IDbDocGenContext dbContext) 
    : IDocumentGeneratorEngine, IScopedService
{
    // Настройки сериализации для преобразования сущностей EF Core в JsonNode
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };
    
    private static IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, IReadOnlyCollection<string>? includes)
        where T : class
    {
        if (includes == null || includes.Count == 0) return query;
        
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        return query;
    }
    
    private static Dictionary<string, object> MapToMiniWordDictionary(MappingConfig mapping, JsonObject dataContext)
    {
        var result = new Dictionary<string, object>();

        // 1. Маппим скалярные значения (например, "{{Group.Specialty}}")
        foreach (var (wordTag, jsonPath) in mapping.Scalars)
        {
            result[wordTag] = ExtractValueByPath(dataContext, jsonPath);
        }

        // 2. Маппим таблицы (списки словарей для MiniWord)
        foreach (var (tableName, tableConfig) in mapping.Tables)
        {
            var listResult = new List<Dictionary<string, object>>();
                
            // Находим массив в нашем JSON (например, "MainGroup.Students")
            var arrayNode = GetNodeByPath(dataContext, tableConfig.SourceArray) as JsonArray;

            if (arrayNode != null)
            {
                int index = 1; // Для нумерации строк (опционально)
                foreach (var itemNode in arrayNode)
                {
                    if (itemNode == null) continue;
                        
                    var rowDict = new Dictionary<string, object>();
                        
                    // Добавляем индекс по умолчанию, часто нужен в документах (№ з/п)
                    rowDict["Number"] = index++; 

                    foreach (var (columnTag, columnPath) in tableConfig.RowMapping)
                    {
                        rowDict[columnTag] = ExtractValueByPath(itemNode, columnPath);
                    }
                        
                    listResult.Add(rowDict);
                }
            }
                
            // MiniWord ожидает список словарей для таблиц
            result[tableName] = listResult;
        }

        return result;
    }

    // Вспомогательный метод обхода JSON по пути (например: "MainGroup.Specialty" или "QualificationWorks[0].Name")
    private static string ExtractValueByPath(JsonNode? node, string path)
    {
        var targetNode = GetNodeByPath(node, path);
        return targetNode?.ToString() ?? string.Empty;
    }

    private static JsonNode? GetNodeByPath(JsonNode? node, string path)
    {
        try
        {
            var parts = path.Split('.');
            JsonNode? current = node;

            foreach (var part in parts)
            {
                if (current == null) return null;

                if (part.EndsWith(']')) 
                {
                    var openBracketIndex = part.IndexOf('[', StringComparison.Ordinal);
                    var closeBracketIndex = part.IndexOf(']', StringComparison.Ordinal);
                    
                    var arrayName = part.Substring(0, openBracketIndex);
                    var indexStr = part.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
                    
                    if (int.TryParse(indexStr, out int index))
                    {
                        current = current[arrayName]?[index];
                    }
                }
                else
                {
                    current = current[part];
                }
            }
            
            return current;
        }
        catch
        {
            return null; 
        }
    }

    public async Task<Stream> GenerateAsync(
        string configurationJson, 
        byte[] wordTemplate, 
        IReadOnlyDictionary<string, string> parameters, 
        CancellationToken cancellationToken)
    {
        // 1. Парсим конфигурацию
        var config = JsonSerializer.Deserialize<TemplateConfiguration>(configurationJson)
            ?? throw new InvalidOperationException("Failed to parse template configuration.");

        // 2. Контейнер для всех данных, вытянутых из БД
        var dataContext = new JsonObject();

        // 3. Динамически выполняем запросы к EF Core
        foreach (var source in config.DataSources)
        {
            var sourceData = await FetchDataAsync(source, parameters, cancellationToken);
            
            // Превращаем результат (граф объектов) в JsonNode и кладем в общий котел по ключу (например, "MainGroup")
            dataContext.Add(source.Key, JsonSerializer.SerializeToNode(sourceData, JsonOptions));
        }

        // 4. Маппим собранный JSON в Dictionary для MiniWord
        var miniWordData = MapToMiniWordDictionary(config.Mapping, dataContext);

        // 5. Генерируем документ
        var memoryStream = new MemoryStream();
        await memoryStream.SaveAsByTemplateAsync(wordTemplate, miniWordData, cancellationToken);

        // 6. КРИТИЧЕСКИ ВАЖНО: Возвращаем каретку в начало, чтобы клиент не скачал пустой файл
        memoryStream.Position = 0;

        return memoryStream;
    }

    private async Task<object> FetchDataAsync(
        DataSourceConfig source, 
        IReadOnlyDictionary<string, string> parameters, 
        CancellationToken cancellationToken)
    {
        // Whitelist паттерн: берем только разрешенные DbSet'ы
        IQueryable query = GetBaseQueryableWithIncludes(source);

        // Применяем динамическую фильтрацию (System.Linq.Dynamic.Core)
        if (!string.IsNullOrWhiteSpace(source.Filter))
        {
            // Собираем аргументы для фильтра (например, берем GroupId из параметров клиента)
            var args = source.FilterArgs
                .Select(argName => parameters.TryGetValue(argName, out var val) ? val : null)
                .Cast<object>()
                .ToArray();

            // Хак для парсинга интов, так как параметры приходят как строки, а в БД могут быть int
            for (int i = 0; i < args.Length; i++)
            {
                if (int.TryParse(args[i]?.ToString(), out int intVal))
                {
                    args[i] = intVal;
                }
            }

            // Выполняем Where, например: .Where("Id == @0", 5)
            query = query.Where(source.Filter, args);
        }

        // Выполняем запрос к БД. ToDynamicListAsync возвращает List<dynamic>.
        // Берем первый элемент, так как DataSourceConfig обычно описывает корень (одну группу, одного ректора).
        var resultList = await query.ToDynamicListAsync(cancellationToken);
        
        return resultList.FirstOrDefault() ?? new object();
    }

    private IQueryable GetBaseQueryableWithIncludes(DataSourceConfig source) => source.Entity switch
    {
        "Group" => ApplyIncludes(dbContext.Groups.AsNoTracking(), source.Includes),
        "Rector" => ApplyIncludes(dbContext.Rectors.AsNoTracking(), source.Includes),
        "Student" => ApplyIncludes(dbContext.Students.AsNoTracking(), source.Includes),
        "Teacher" => ApplyIncludes(dbContext.Teachers.AsNoTracking(), source.Includes),
        "QualificationWork" => ApplyIncludes(dbContext.QualificationWorks.AsNoTracking(), source.Includes),
        _ => throw new NotSupportedException($"Entity '{source.Entity}' is not allowed.")
    };
}
