using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Department : BaseEntity
{
    public string FullName { get; set; }

    public ICollection<Specialty> Specialties { get; init; } = new HashSet<Specialty>();

    private Department() { FullName = string.Empty; }
    public Department(string fullName) { FullName = fullName; }
}
