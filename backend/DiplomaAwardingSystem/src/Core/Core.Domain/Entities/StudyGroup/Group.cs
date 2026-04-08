using Core.Domain.Enums;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Group : BaseEntity
{
    public string Name { get; init; }
    public string Year { get; init; }
    public EducationLevel EducationLevel { get; init; }
    public int SpecialtyId { get; init; }
    public Specialty? Specialty { get; init; }

    private Group()
    {
        Name = string.Empty;
        Year = string.Empty;
        EducationLevel = EducationLevel.None;
    }

    public Group(string name, string year, EducationLevel educationLevel, int specialtyId)
    {
        Name = name;
        Year = year;
        EducationLevel = educationLevel;
        SpecialtyId = specialtyId;
    }
}
