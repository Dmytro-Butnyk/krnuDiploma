namespace Core.Domain.Entities.TeacherStaff;

public sealed class TeacherPosition : BaseEntity
{
    public string FullName { get; set; }
    public string ShortName { get; set; }

    public ICollection<Teacher> Teachers { get; init; } = new HashSet<Teacher>();

    private TeacherPosition()
    {
        FullName = string.Empty;
        ShortName = string.Empty;
    }

    public TeacherPosition(string fullName, string shortName)
    {
        FullName = fullName;
        ShortName = shortName;
    }
}
