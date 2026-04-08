using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class Teacher : BaseEntity
{
    public string FullName { get; init; }
    public string ShortName { get; init; }
    public string Email { get; init; }
    public string PhoneNumber { get; init; }
    public string Position { get; init; }
    public int AcademicDegreeId { get; init; }
    public AcademicDegree? AcademicDegree { get; init; }
    public int DepartmentId { get; init; }
    public Department? Department { get; init; }

    private Teacher()
    {
        FullName = string.Empty;
        ShortName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        Position = string.Empty;
    }

    public Teacher(string fullName, string shortName, string email, string phoneNumber,
        string position, int academicDegreeId, int departmentId)
    {
        FullName = fullName;
        ShortName = shortName;
        Email = email;
        PhoneNumber = phoneNumber;
        Position = position;
        AcademicDegreeId = academicDegreeId;
        DepartmentId = departmentId;
    }
}
