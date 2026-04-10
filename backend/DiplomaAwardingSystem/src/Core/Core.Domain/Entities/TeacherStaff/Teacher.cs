using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.ArchiveGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class Teacher : BaseEntity
{
    public string FullName { get; init; }
    public string ShortName { get; init; }
    public string Email { get; init; }
    public string PhoneNumber { get; init; }
    public string Position { get; init; }
    
    // N-to-1 with AcademicDegree
    public int AcademicDegreeId { get; init; }
    public AcademicDegree? AcademicDegree { get; init; }
    
    // N-to-1 with Department
    public int DepartmentId { get; init; }
    public Department? Department { get; init; }

    // Collections
    public ICollection<QualificationWork> QualificationWorks { get; init; } = new HashSet<QualificationWork>();
    
    // 1 teacher can be in many commissions (acting as DecMember)
    public ICollection<DecMember> DecMembers { get; init; } = new HashSet<DecMember>();

    private Teacher()
    {
        FullName = string.Empty;
        ShortName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        Position = string.Empty;
    }

    public Teacher(string fullName, string shortName, string email, string phoneNumber, string position, int academicDegreeId, int departmentId)
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
