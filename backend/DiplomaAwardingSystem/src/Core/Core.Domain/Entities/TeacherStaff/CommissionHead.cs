using Core.Domain.Entities;
using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class CommissionHead : BaseEntity
{
    public string FullName { get; set; }
    public PersonNameForms NameForms { get; set; }
    public string Position { get; set; }
    public string Company { get; set; }
    public string Specialty { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<DiplomaExaminationCommission> DiplomaExaminationCommissions { get; init; } = new HashSet<DiplomaExaminationCommission>();

    private CommissionHead()
    {
        FullName = string.Empty;
        NameForms = PersonNameForms.FromDefault(string.Empty);
        Position = string.Empty;
        Company = string.Empty;
        Specialty = string.Empty;
    }

    public CommissionHead(string fullName, string position, string company, string specialty)
    {
        FullName = fullName.Trim();
        NameForms = PersonNameForms.FromDefault(FullName);
        Position = position;
        Company = company;
        Specialty = specialty;
    }
}
