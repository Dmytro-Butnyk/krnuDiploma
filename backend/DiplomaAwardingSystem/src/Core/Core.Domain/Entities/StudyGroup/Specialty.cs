using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Specialty : BaseEntity
{
    public string Code { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Group> Groups { get; init; } = new HashSet<Group>();
    public ICollection<Teacher> Teachers { get; init; } = new HashSet<Teacher>();
    public ICollection<Secretary> Secretaries { get; init; } = new HashSet<Secretary>();
    public ICollection<DiplomaExaminationCommission> DiplomaExaminationCommissions { get; init; } = new HashSet<DiplomaExaminationCommission>();

    private Specialty()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public Specialty(string code, string name)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }
}
