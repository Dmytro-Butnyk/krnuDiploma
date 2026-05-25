namespace DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions;

public static class DiplomaExaminationCommissionContracts
{
    public sealed record GroupDto(int Id, string Name);

    public sealed record TeacherDto(
        int Id,
        string FullName,
        string Position);

    public sealed record SecretaryDto(int Id, string FullName);

    public sealed record CommissionHeadDto(
        int Id,
        string FullName,
        string Position,
        string Company,
        string Specialty,
        bool IsDeleted);

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
        CommissionHeadDto Head,
        IReadOnlyCollection<MemberDto> Members,
        SecretaryDto Secretary,
        IReadOnlyCollection<GroupDto> Groups);

    public abstract class DiplomaExaminationCommissionUpdateRequest
    {
        public string SecretaryEmail { get; init; } = string.Empty;
        public int SecretaryId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public int CommissionHeadId { get; init; }
        public int FirstMemberTeacherId { get; init; }
        public int SecondMemberTeacherId { get; init; }
        public int ThirdMemberTeacherId { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
    }

    public abstract class DiplomaExaminationCommissionCreateRequest : DiplomaExaminationCommissionUpdateRequest
    {
        public string EducationLevel { get; init; } = string.Empty;
        public string DefenseYear { get; init; } = string.Empty;
    }
}
