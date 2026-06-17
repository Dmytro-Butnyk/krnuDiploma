using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.Enums;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class DiplomaExaminationCommission : BaseEntity
{
    public string OrderNumber { get; set; }
    public EducationLevel EducationLevel { get; set; }
    public string DefenseYear { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string MeetingStart { get; set; }
    public string MeetingEnd { get; set; }

    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    public int CommissionHeadId { get; set; }
    public CommissionHead? CommissionHead { get; set; }

    public int FirstMemberTeacherId { get; set; }
    public Teacher? FirstMemberTeacher { get; set; }

    public int SecondMemberTeacherId { get; set; }
    public Teacher? SecondMemberTeacher { get; set; }

    public int ThirdMemberTeacherId { get; set; }
    public Teacher? ThirdMemberTeacher { get; set; }

    public int? FirstConsultantId { get; set; }
    public Teacher? FirstConsultant { get; set; }

    public int? SecondConsultantId { get; set; }
    public Teacher? SecondConsultant { get; set; }

    public int SecretaryId { get; set; }
    public Secretary? Secretary { get; set; }

    public Archive? Archive { get; set; }
    public ICollection<Group> Groups { get; init; } = new HashSet<Group>();

    private DiplomaExaminationCommission()
    {
        OrderNumber = string.Empty;
        DefenseYear = string.Empty;
        MeetingStart = string.Empty;
        MeetingEnd = string.Empty;
    }

    public DiplomaExaminationCommission(
        string orderNumber,
        EducationLevel educationLevel,
        string defenseYear,
        DateOnly startDate,
        DateOnly endDate,
        string meetingStart,
        string meetingEnd,
        int specialtyId,
        int commissionHeadId,
        int firstMemberTeacherId,
        int secondMemberTeacherId,
        int thirdMemberTeacherId,
        int? firstConsultantId,
        int? secondConsultantId,
        int secretaryId)
    {
        OrderNumber = orderNumber;
        EducationLevel = educationLevel;
        DefenseYear = defenseYear;
        StartDate = startDate;
        EndDate = endDate;
        MeetingStart = meetingStart;
        MeetingEnd = meetingEnd;
        SpecialtyId = specialtyId;
        CommissionHeadId = commissionHeadId;
        FirstMemberTeacherId = firstMemberTeacherId;
        SecondMemberTeacherId = secondMemberTeacherId;
        ThirdMemberTeacherId = thirdMemberTeacherId;
        FirstConsultantId = firstConsultantId;
        SecondConsultantId = secondConsultantId;
        SecretaryId = secretaryId;
    }
}
