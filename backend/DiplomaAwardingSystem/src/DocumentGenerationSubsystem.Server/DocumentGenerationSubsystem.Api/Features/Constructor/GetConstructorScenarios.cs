using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DocumentGenerationSubsystem.Api.Features.Constructor;

public static class GetConstructorScenarios
{
    public sealed record ConstructorScenarioDto(
        string Id,
        string Title,
        string Description,
        IReadOnlyDictionary<string, InputConfig> Inputs,
        IReadOnlyCollection<DataSourceConfig> DataSources,
        IReadOnlyCollection<ScenarioTableSourceDto> RecommendedTableSources,
        IReadOnlyCollection<ScenarioScalarMappingDto> RequiredScalarMappings,
        IReadOnlyCollection<ScenarioTableRequirementDto> RequiredTableSources);

    public sealed record ScenarioTableSourceDto(string Key, string Label, string Entity);

    public sealed record ScenarioScalarMappingDto(string Tag, string Path, string Message);

    public sealed record ScenarioTableRequirementDto(string SourceArray, string Message);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/constructor/scenarios", Handle)
                .WithSummary("Gets predefined document data scenarios for the template constructor")
                .Produces<IReadOnlyCollection<ConstructorScenarioDto>>(StatusCodes.Status200OK)
                .WithTags("TemplateConstructor");
        }

        private static Ok<IReadOnlyCollection<ConstructorScenarioDto>> Handle()
        {
            return TypedResults.Ok<IReadOnlyCollection<ConstructorScenarioDto>>(
            [
                CreateGroupDefenceDayExtractScenario()
            ]);
        }
    }

    private static ConstructorScenarioDto CreateGroupDefenceDayExtractScenario()
    {
        Dictionary<string, InputConfig> inputs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["GroupId"] = new InputConfig(
                InputKinds.EntitySelect,
                InputValueTypes.Int,
                "Група",
                true,
                "Group",
                null,
                [],
                [],
                ["Name", "Year"],
                [],
                ["Name", "Year"],
                ["Year desc", "Name"],
                null),
            ["DefenceDate"] = new InputConfig(
                InputKinds.ValueSelect,
                InputValueTypes.Date,
                "Дата захисту",
                true,
                "QualificationWork",
                "DefenceDate",
                ["GroupId"],
                [
                    new InputFilterConfig("Student.GroupId", "Equals", "GroupId")
                ],
                [],
                [],
                [],
                ["DefenceDate"],
                null),
        };

        DataSourceConfig[] dataSources =
        [
            new(
                "TargetGroup",
                "Group",
                DataSourceResults.One,
                "Id == @0",
                ["GroupId"],
                [
                    "Specialty",
                    "DiplomaExaminationCommission.Secretary"
                ],
                []),
            new(
                "DayStudents",
                "Student",
                DataSourceResults.Many,
                "GroupId == @0 && QualificationWork.DefenceDate == @1 && QualificationWork.CommissionScore >= 60",
                ["GroupId", "DefenceDate"],
                [
                    "Group.Specialty",
                    "QualificationWork"
                ],
                ["FullName"])
        ];

        return new ConstructorScenarioDto(
            "group-defence-day-extract",
            "Витяг за день захисту групи",
            "Група, дата захисту, студенти цієї дати з оцінкою 60+. Ручні поля документа налаштовуються окремо на маппінгу.",
            inputs,
            dataSources,
            [
                new ScenarioTableSourceDto("DayStudents", "Студенти за дату захисту", "Student")
            ],
            [
                new ScenarioScalarMappingDto(
                    "DefenceDate",
                    "Input.DefenceDate",
                    "Шаблон має містити тег {{DefenceDate}}, бо дата захисту вибирається сценарієм і використовується для фільтрації студентів."),
                new ScenarioScalarMappingDto(
                    "ProtocolsNumbers",
                    "Computed.ProtocolsNumbers",
                    "Шаблон має містити тег {{ProtocolsNumbers}}, бо діапазон протоколів обчислюється сценарієм за групою, датою захисту та студентами з оцінкою 60+.")
            ],
            [
                new ScenarioTableRequirementDto(
                    "DayStudents",
                    "Таблиця студентів має бути прив'язана до SourceArray DayStudents. Не використовуйте TargetGroup.Students, бо це обійде фільтр за датою і оцінкою.")
            ]);
    }
}
