namespace Core.Domain.Entities.StudyGroup;

public sealed class Specialty : BaseEntity
{
    public string Code { get; init; }
    public string Name { get; init; }
    public int DepartmentId { get; init; }
    public Department? Department { get; init; }

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
