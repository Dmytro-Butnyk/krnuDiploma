using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Specialty : BaseEntity
{
    public string Code { get; set; }
    public string Name { get; set; }
    
    // N-to-1 with Department
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<Group> Groups { get; init; } = new HashSet<Group>();
    public ICollection<Teacher> Teachers { get; init; } = new HashSet<Teacher>();
    public ICollection<Secretary> Secretaries { get; init; } = new HashSet<Secretary>();

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
