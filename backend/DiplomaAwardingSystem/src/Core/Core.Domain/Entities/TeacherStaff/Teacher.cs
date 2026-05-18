using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.ArchiveGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class Teacher : BaseEntity
{
    public string FullName { get; set; }
    public string ShortName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Position { get; set; }
    
    // N-to-1 with AcademicDegree
    public int AcademicDegreeId { get; set; }
    public AcademicDegree? AcademicDegree { get; set; }
    
    // N-to-1 with Specialty
    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

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

    public Teacher(string fullName, string shortName, string email, string phoneNumber, string position, int academicDegreeId, int specialtyId)
    {
        FullName = fullName;
        ShortName = shortName;
        Email = email;
        PhoneNumber = phoneNumber;
        Position = position;
        AcademicDegreeId = academicDegreeId;
        SpecialtyId = specialtyId;
    }
}
