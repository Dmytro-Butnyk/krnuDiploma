using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.StudyGroup;

public sealed class Department : BaseEntity
{
    public string FullName { get; init; }

    public ICollection<Specialty> Specialties { get; init; } = new HashSet<Specialty>();
    public ICollection<Teacher> Teachers { get; init; } = new HashSet<Teacher>();

    private Department() { FullName = string.Empty; }
    public Department(string fullName) { FullName = fullName; }
}
