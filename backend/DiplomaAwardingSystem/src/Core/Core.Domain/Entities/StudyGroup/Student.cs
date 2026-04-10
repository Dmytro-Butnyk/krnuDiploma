using Core.Domain.Entities.ArchiveGroup;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Student : BaseEntity
{
    public string FullName { get; init; }
    
    // N-to-1 with Group
    public int GroupId { get; init; }
    public Group? Group { get; init; }

    // 1-to-1 with QualificationWork
    public QualificationWork? QualificationWork { get; init; }

    private Student() { FullName = string.Empty; }
    
    public Student(string fullName, int groupId)
    {
        FullName = fullName;
        GroupId = groupId;
    }
}
