using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class Teacher : BaseEntity
{
    public string FullName { get; set; }
    public string ShortName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Position { get; set; }

    public int AcademicDegreeId { get; set; }
    public AcademicDegree? AcademicDegree { get; set; }

    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    public ICollection<QualificationWork> QualificationWorks { get; init; } = new HashSet<QualificationWork>();
    public ICollection<QualificationWork> ReviewedQualificationWorks { get; init; } = new HashSet<QualificationWork>();

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
