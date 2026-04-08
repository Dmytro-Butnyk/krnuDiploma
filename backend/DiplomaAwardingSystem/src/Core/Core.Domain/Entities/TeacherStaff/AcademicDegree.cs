namespace Core.Domain.Entities.TeacherStaff;

public sealed class AcademicDegree : BaseEntity
{
    public string FullName { get; init; }
    public string ShortName { get; init; }

    private AcademicDegree()
    {
        FullName = string.Empty;
        ShortName = string.Empty;
    }

    public AcademicDegree(string fullName, string shortName)
    {
        FullName = fullName;
        ShortName = shortName;
    }
}
