using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.Enums;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class DiplomaExaminationCommission : BaseEntity
{
    public int OrderNumber { get; set; }
    public EducationLevel EducationLevel { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public int? HeadTeacherId { get; set; }
    public Teacher? HeadTeacher { get; set; }
    public string? HeadPersonaName { get; set; }
    public string? HeadPersonaPosition { get; set; }

    public int FirstMemberTeacherId { get; set; }
    public Teacher? FirstMemberTeacher { get; set; }

    public int SecondMemberTeacherId { get; set; }
    public Teacher? SecondMemberTeacher { get; set; }

    public int ThirdMemberTeacherId { get; set; }
    public Teacher? ThirdMemberTeacher { get; set; }

    public int SecretaryId { get; set; }
    public Secretary? Secretary { get; set; }

    public Archive? Archive { get; set; }
    public ICollection<Group> Groups { get; init; } = new HashSet<Group>();

    private DiplomaExaminationCommission() { }

    public DiplomaExaminationCommission(
        int orderNumber,
        EducationLevel educationLevel,
        DateOnly startDate,
        DateOnly endDate,
        int? headTeacherId,
        string? headPersonaName,
        string? headPersonaPosition,
        int firstMemberTeacherId,
        int secondMemberTeacherId,
        int thirdMemberTeacherId,
        int secretaryId)
    {
        OrderNumber = orderNumber;
        EducationLevel = educationLevel;
        StartDate = startDate;
        EndDate = endDate;
        HeadTeacherId = headTeacherId;
        HeadPersonaName = headPersonaName;
        HeadPersonaPosition = headPersonaPosition;
        FirstMemberTeacherId = firstMemberTeacherId;
        SecondMemberTeacherId = secondMemberTeacherId;
        ThirdMemberTeacherId = thirdMemberTeacherId;
        SecretaryId = secretaryId;
    }
}
