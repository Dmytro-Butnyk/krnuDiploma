namespace Core.Domain.Entities.TeacherStaff;

public sealed class TeacherPosition : BaseEntity
{
    public string FullName { get; set; }
    public string ShortName { get; set; }
    public string GenitiveFullName { get; set; }
    public string GenitiveShortName { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Teacher> Teachers { get; init; } = new HashSet<Teacher>();

    private TeacherPosition()
    {
        FullName = string.Empty;
        ShortName = string.Empty;
        GenitiveFullName = string.Empty;
        GenitiveShortName = string.Empty;
    }

    public TeacherPosition(string fullName, string shortName)
    {
        FullName = fullName.Trim();
        ShortName = shortName.Trim();
        GenitiveFullName = FullName;
        GenitiveShortName = ShortName;
        IsActive = true;
    }
}
