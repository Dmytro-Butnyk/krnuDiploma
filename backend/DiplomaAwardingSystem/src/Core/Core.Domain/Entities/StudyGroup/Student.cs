using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudentDiplomaData;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Student : BaseEntity
{
    public string FullName { get; set; }
    
    // N-to-1 with Group
    public int GroupId { get; set; }
    public Group? Group { get; set; }

    // 1-to-1 with QualificationWork
    public QualificationWork? QualificationWork { get; set; }

    // 1-to-1 with ElectronicComponentsChecklist
    public ElectronicComponentsChecklist? ElectronicComponentsChecklist { get; set; }

    // 1-to-1 with PhysicalComponentsChecklist
    public PhysicalComponentsChecklist? PhysicalComponentsChecklist { get; set; }

    private Student() { FullName = string.Empty; }
    
    public Student(string fullName, int groupId)
    {
        FullName = fullName;
        GroupId = groupId;
    }
}
