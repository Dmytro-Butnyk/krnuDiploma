using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities;
using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class Teacher : BaseEntity
{
    public string FullName { get; set; }
    public string ShortName { get; set; }
    public PersonNameForms NameForms { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsActive { get; set; }

    public int AcademicDegreeId { get; set; }
    public AcademicDegree? AcademicDegree { get; set; }

    public int TeacherPositionId { get; set; }
    public TeacherPosition? TeacherPosition { get; set; }

    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    public ICollection<QualificationWork> QualificationWorks { get; init; } = new HashSet<QualificationWork>();
    public ICollection<QualificationWork> ReviewedQualificationWorks { get; init; } = new HashSet<QualificationWork>();

    private Teacher()
    {
        FullName = string.Empty;
        ShortName = string.Empty;
        NameForms = PersonNameForms.FromDefault(string.Empty);
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    public Teacher(
        string fullName,
        string shortName,
        string email,
        string phoneNumber,
        int academicDegreeId,
        int teacherPositionId,
        int specialtyId)
    {
        FullName = fullName.Trim();
        ShortName = shortName.Trim();
        NameForms = PersonNameForms.FromDefault(FullName, ShortName);
        Email = email;
        PhoneNumber = phoneNumber;
        AcademicDegreeId = academicDegreeId;
        TeacherPositionId = teacherPositionId;
        SpecialtyId = specialtyId;
        IsActive = true;
    }
}
