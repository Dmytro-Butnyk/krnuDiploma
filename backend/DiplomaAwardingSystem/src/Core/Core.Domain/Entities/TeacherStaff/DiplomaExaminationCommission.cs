using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class DiplomaExaminationCommission : BaseEntity
{
    public int OrderNumber { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    
    // 1-к-1 связь
    public int GroupId { get; init; }
    public Group? Group { get; init; }

    public Archive? Archive { get; init; }
    public ICollection<Defence> Defences { get; init; } = new HashSet<Defence>();
    
    // Связь через таблицу-посредник (Many-to-Many)
    public ICollection<DecToMember> DecToMembers { get; init; } = new HashSet<DecToMember>();

    private DiplomaExaminationCommission() { }

    public DiplomaExaminationCommission(int orderNumber, DateOnly startDate, DateOnly endDate, int groupId)
    {
        OrderNumber = orderNumber;
        StartDate = startDate;
        EndDate = endDate;
        GroupId = groupId;
    }
}
