namespace DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions;

public static class DiplomaExaminationCommissionContracts
{
    public sealed record GroupDto(int Id, string Name);

    public sealed record TeacherDto(
        int Id,
        string FullName,
        string Position);

    public sealed record SecretaryDto(int Id, string FullName);

    public sealed record PersonDto(
        string FullName,
        string? Position);

    public sealed record HeadDto(
        TeacherDto? Teacher,
        PersonDto? Person);

    public sealed record MemberDto(
        int TeacherId,
        string FullName,
        string Position);

    public sealed record DiplomaExaminationCommissionResponse(
        int Id,
        string OrderNumber,
        string EducationLevel,
        string Year,
        string DefenseYear,
        DateOnly StartDate,
        DateOnly EndDate,
        HeadDto Head,
        IReadOnlyCollection<MemberDto> Members,
        SecretaryDto Secretary,
        IReadOnlyCollection<GroupDto> Groups);

    public abstract class DiplomaExaminationCommissionUpsertRequest
    {
        public string SecretaryEmail { get; init; } = string.Empty;
        public int SecretaryId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public string EducationLevel { get; init; } = string.Empty;
        public string DefenseYear { get; init; } = string.Empty;
        public IReadOnlyCollection<int> GroupIds { get; init; } = Array.Empty<int>();
        public int? HeadTeacherId { get; init; }
        public string? HeadPersonaName { get; init; }
        public string? HeadPersonaPosition { get; init; }
        public int FirstMemberTeacherId { get; init; }
        public int SecondMemberTeacherId { get; init; }
        public int ThirdMemberTeacherId { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
    }
}
