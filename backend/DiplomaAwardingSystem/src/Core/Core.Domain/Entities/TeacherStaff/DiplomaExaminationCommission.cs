using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class DiplomaExaminationCommission : BaseEntity
{
     public int OrderNumber { get; init; }
     public DateOnly StartDate { get; init; }
     public DateOnly EndDate { get; init; }
     public int GroupId { get; init; }
     public Group? Group { get; init; }

     private DiplomaExaminationCommission()
     {
     }

     public DiplomaExaminationCommission(int orderNumber, DateOnly startDate, DateOnly endDate, int groupId)
     {
         OrderNumber = orderNumber;
         StartDate = startDate;
         EndDate = endDate;
         GroupId = groupId;
     }
}
