using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class DiplomaExaminationCommission : BaseEntity
{
    public int OrderNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    
    // 1-к-1 связь
    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public Archive? Archive { get; set; }
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
