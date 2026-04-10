namespace Core.Domain.Entities.StudyGroup;

public sealed class Specialty : BaseEntity
{
    public string Code { get; init; }
    public string Name { get; init; }
    
    // N-to-1 with Department
    public int DepartmentId { get; init; }
    public Department? Department { get; init; }

    public ICollection<Group> Groups { get; init; } = new HashSet<Group>();

    private Specialty()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public Specialty(string code, string name, int departmentId)
    {
        Code = code;
        Name = name;
        DepartmentId = departmentId;
    }
}
