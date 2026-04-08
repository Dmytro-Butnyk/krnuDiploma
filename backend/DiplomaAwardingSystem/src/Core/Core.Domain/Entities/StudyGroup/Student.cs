namespace Core.Domain.Entities.StudyGroup;

public sealed class Student : BaseEntity
{
    public string FullName { get; init; }
    public int GroupId { get; init; }
    public Group? Group { get; init; }

    private Student()
    {
        FullName = string.Empty;
    }
    
    public Student(string fullName, int groupId)
    {
        FullName = fullName;
        GroupId = groupId;
    }
}
