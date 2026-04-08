namespace Core.Domain.Entities.StudyGroup;

public sealed class Department : BaseEntity
{
    public string FullName { get; init; }

    private Department()
    {
        FullName = string.Empty;
    }

    public Department(string fullName)
    {
        FullName = fullName;
    }
}
