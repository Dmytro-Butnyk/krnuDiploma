using Core.Domain.Entities.TeacherStaff;
using Core.Domain.Enums;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Group : BaseEntity
{
    public string Name { get; set; }
    public string Year { get; set; }
    public EducationLevel EducationLevel { get; set; }
    
    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    // Изменено: теперь это 1-к-1 (одна группа - одна комиссия)
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; set; }
    
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
