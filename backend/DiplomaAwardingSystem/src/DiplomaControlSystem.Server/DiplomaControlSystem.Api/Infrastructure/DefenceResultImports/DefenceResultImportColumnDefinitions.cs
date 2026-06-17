using DiplomaControlSystem.Api.Infrastructure.ImportColumns;

namespace DiplomaControlSystem.Api.Infrastructure.DefenceResultImports;

internal static class DefenceResultImportColumnDefinitions
{
    internal static readonly ImportColumnDefinition StudentFullName = new(
        "studentFullName",
        "\u041f\u0406\u0411 \u0441\u0442\u0443\u0434\u0435\u043d\u0442\u0430",
        Required: true,
        new[]
        {
            "\u041f\u0406\u0411 \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
            "STUDENT",
            "STUDENT NAME",
            "FULL NAME",
            "FULLNAME"
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

    internal static readonly ImportColumnDefinition Plagiarism = new(
        "plagiarismPercent",
        "\u041f\u0440\u043e\u0446\u0435\u043d\u0442 \u0437\u0430\u043f\u043e\u0437\u0438\u0447\u0435\u043d\u044c",
        Required: false,
        new[]
        {
            "\u041f\u0420\u041e\u0426\u0415\u041d\u0422 \u0417\u0410\u041f\u041e\u0417\u0418\u0427\u0415\u041d\u042c",
            "PLAGIARISM PERCENT"
        });

    internal static readonly ImportColumnDefinition CommissionScore = new(
        "commissionScore",
        "\u0417\u0430\u0433\u0430\u043b\u044c\u043d\u0430 \u043e\u0446\u0456\u043d\u043a\u0430",
        Required: false,
        new[]
        {
            "\u0417\u0410\u0413\u0410\u041b\u042c\u041d\u0410 \u041e\u0426\u0406\u041d\u041a\u0410",
            "COMMISSION SCORE",
            "TOTAL SCORE"
        });

    internal static readonly ImportColumnDefinition DefenceDate = new(
        "defenceDate",
        "\u0414\u0430\u0442\u0430 \u0437\u0430\u0445\u0438\u0441\u0442\u0443",
        Required: false,
        new[]
        {
            "\u0414\u0410\u0422\u0410 \u0417\u0410\u0425\u0418\u0421\u0422\u0423",
            "DEFENCE DATE",
            "DEFENSE DATE"
        });

    internal static readonly IReadOnlyCollection<ImportColumnDefinition> All = new[]
    {
        StudentFullName,
        Supervisor,
        Plagiarism,
        CommissionScore,
        DefenceDate
    };

    internal static readonly HashSet<string> StudentFullNameHeaderNames = CreateHeaderSet(StudentFullName);
    internal static readonly HashSet<string> SupervisorHeaderNames = CreateHeaderSet(Supervisor);
    internal static readonly HashSet<string> PlagiarismHeaderNames = CreateHeaderSet(Plagiarism);
    internal static readonly HashSet<string> CommissionScoreHeaderNames = CreateHeaderSet(CommissionScore);
    internal static readonly HashSet<string> DefenceDateHeaderNames = CreateHeaderSet(DefenceDate);

    private static HashSet<string> CreateHeaderSet(ImportColumnDefinition column)
    {
        return new HashSet<string>(column.AcceptedHeaders, StringComparer.Ordinal);
    }
}
