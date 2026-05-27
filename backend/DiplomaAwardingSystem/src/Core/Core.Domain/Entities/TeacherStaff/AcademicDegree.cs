namespace Core.Domain.Entities.TeacherStaff;

public sealed class AcademicDegree : BaseEntity
{
    public string FullName { get; set; }
    public string ShortName { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Teacher> Teachers { get; init; } = new HashSet<Teacher>();

    private AcademicDegree()
    {
        FullName = string.Empty;
        ShortName = string.Empty;
    }

    public AcademicDegree(string fullName, string shortName)
    {
        FullName = fullName;
        ShortName = shortName;
        IsActive = true;
    }
}
