using Core.Domain.Enums;

namespace Core.Domain.Entities;

public sealed class Student : BaseEntity
{
    public string FullName { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public int GroupId { get; init; }
    public Group? Group { get; init; }
    
    public QualificationWork? QualificationWork { get; init; }

    private Student()
    {
    }

    public Student(string fullName, Gender gender, int groupId)
    {
        FullName = fullName;
        Gender = gender;
        GroupId = groupId;
    }
}
