using Core.Domain.Entities.TeacherStaff;
using Core.Domain.Enums;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Group : BaseEntity
{
    public string Name { get; init; }
    public string Year { get; init; }
    public EducationLevel EducationLevel { get; init; }
    
    public int SpecialtyId { get; init; }
    public Specialty? Specialty { get; init; }

    // Изменено: теперь это 1-к-1 (одна группа - одна комиссия)
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; init; }
    
    public ICollection<Student> Students { get; init; } = new HashSet<Student>();

    private Group()
    {
        Name = string.Empty;
        Year = string.Empty;
    }

    public Group(string name, string year, EducationLevel educationLevel, int specialtyId)
    {
        Name = name;
        Year = year;
        EducationLevel = educationLevel;
        SpecialtyId = specialtyId;
    }
}
