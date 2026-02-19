namespace Core.Domain.Entities;

public sealed class Group : BaseEntity
{
    public string Name { get; init; }
    public string Specialty { get; init; }
    public int TeacherId { get; init; }
    public Teacher? Teacher { get; init; }
    
    public ICollection<Student> Students { get; init; } = new List<Student>();
    
    private Group()
    {
        Name = string.Empty;
        Specialty = string.Empty;
    }

    public Group(string name, string specialty, int teacherId)
    {
        Name = name;
        Specialty = specialty;
        TeacherId = teacherId;
    }
}
