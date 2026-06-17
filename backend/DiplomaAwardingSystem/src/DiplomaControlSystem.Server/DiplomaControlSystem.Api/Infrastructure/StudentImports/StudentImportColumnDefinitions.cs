using DiplomaControlSystem.Api.Infrastructure.ImportColumns;

namespace DiplomaControlSystem.Api.Infrastructure.StudentImports;

internal static class StudentImportColumnDefinitions
{
    internal static readonly ImportColumnDefinition StudentFullName = new(
        "studentFullName",
        "\u041f\u0406\u0411 \u0441\u0442\u0443\u0434\u0435\u043d\u0442\u0430",
        Required: true,
        new[]
        {
            "FULLNAME",
            "FULL NAME",
            "STUDENT",
            "STUDENT NAME",
            "NAME",
            "\u041f\u0406\u0411",
            "\u0424\u0418\u041e",
            "\u0406\u041c'\u042f \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
            "\u0406\u041c\u042f \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
            "\u0418\u041c\u042f \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
            "\u041f\u0420\u0406\u0417\u0412\u0418\u0429\u0415 \u0406\u041c'\u042f \u041f\u041e \u0411\u0410\u0422\u042c\u041a\u041e\u0412\u0406",
            "\u041f\u0420\u0406\u0417\u0412\u0418\u0429\u0415 \u0406\u041c\u042f \u041f\u041e \u0411\u0410\u0422\u042c\u041A\u041E\u0412\u0406",
            "\u041f\u0406\u0411 \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410"
        });

    internal static readonly ImportColumnDefinition Supervisor = new(
        "supervisor",
        "\u041f\u0406\u0411 \u043a\u0435\u0440\u0456\u0432\u043d\u0438\u043a\u0430",
        Required: false,
        new[]
        {
            "\u041f\u0406\u0411 \u041a\u0415\u0420\u0406\u0412\u041d\u0418\u041a\u0410",
            "\u041a\u0415\u0420\u0406\u0412\u041d\u0418\u041a",
            "SUPERVISOR",
            "SUPERVISOR NAME"
        });

    internal static readonly ImportColumnDefinition Topic = new(
        "topic",
        "\u0422\u0435\u043c\u0430 \u0440\u043e\u0431\u043e\u0442\u0438",
        Required: false,
        new[]
        {
            "\u0422\u0415\u041c\u0410 \u0420\u041e\u0411\u041e\u0422\u0418",
            "\u0422\u0415\u041c\u0410",
            "TOPIC",
            "WORK TOPIC"
        });

    internal static readonly ImportColumnDefinition PracticeBase = new(
        "practiceBase",
        "\u041c\u0456\u0441\u0446\u0435 \u043f\u0440\u0430\u043a\u0442\u0438\u043a\u0438",
        Required: false,
        new[]
        {
            "\u041c\u0406\u0421\u0426\u0415 \u041f\u0420\u0410\u041a\u0422\u0418\u041a\u0418",
            "\u041c\u0406\u0421\u0426\u0415 \u041f\u0420\u041e\u0425\u041e\u0414\u0416\u0415\u041d\u041d\u042f \u041f\u0420\u0410\u041a\u0422\u0418\u041a\u0418",
            "PRACTICE BASE",
            "PRACTICE PLACE"
        });

    internal static readonly IReadOnlyCollection<ImportColumnDefinition> All = new[]
    {
        StudentFullName,
        Supervisor,
        Topic,
        PracticeBase
    };

    internal static readonly HashSet<string> StudentFullNameHeaderNames = CreateHeaderSet(StudentFullName);
    internal static readonly HashSet<string> SupervisorHeaderNames = CreateHeaderSet(Supervisor);
    internal static readonly HashSet<string> TopicHeaderNames = CreateHeaderSet(Topic);
    internal static readonly HashSet<string> PracticeBaseHeaderNames = CreateHeaderSet(PracticeBase);

    private static HashSet<string> CreateHeaderSet(ImportColumnDefinition column)
    {
        return new HashSet<string>(column.AcceptedHeaders, StringComparer.Ordinal);
    }
}
