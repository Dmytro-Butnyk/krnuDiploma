using static DiplomaControlSystem.Api.Contracts.CommissionHeads.CommissionHeadContracts;

namespace DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions;

public static class DiplomaExaminationCommissionContracts
{
    public sealed record GroupDto(int Id, string Name);

    public sealed record TeacherDto(
        int Id,
        string FullName,
        string Position);

    public sealed record SecretaryDto(int Id, string FullName);

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
        string MeetingStart,
        string MeetingEnd,
        CommissionHeadDto Head,
        IReadOnlyCollection<MemberDto> Members,
        MemberDto? FirstConsultant,
        MemberDto? SecondConsultant,
        SecretaryDto Secretary,
        IReadOnlyCollection<GroupDto> Groups);

    public abstract class DiplomaExaminationCommissionUpdateRequest
    {
        public int SecretaryId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public int CommissionHeadId { get; init; }
        public int FirstMemberTeacherId { get; init; }
        public int SecondMemberTeacherId { get; init; }
        public int ThirdMemberTeacherId { get; init; }
        public int? FirstConsultantId { get; init; }
        public int? SecondConsultantId { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public string MeetingStart { get; init; } = string.Empty;
        public string MeetingEnd { get; init; } = string.Empty;
    }

    public abstract class DiplomaExaminationCommissionCreateRequest : DiplomaExaminationCommissionUpdateRequest
    {
        public string EducationLevel { get; init; } = string.Empty;
        public string DefenseYear { get; init; } = string.Empty;
    }
}
