namespace Core.Domain.Entities;

public sealed class Student : BaseEntity
{
    public required string FullName { get; set; }
    public required string Sex { get; set; }
    public required int GroupId { get; set; }
    public required Group Group { get; set; }

    private Student()
    {
    }

    public Student(string fullName, string sex, int groupId)
    {
        FullName = fullName;
        Sex = sex;
        GroupId = groupId;
    }
    
    public Student(string fullName, string sex, Group group)
    {
        FullName = fullName;
        Sex = sex;
        Group = group;
    }
}
