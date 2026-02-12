namespace Core.Domain.Entities;

public sealed class Group : BaseEntity
{
    public required string Name { get; set; }
    public required string Specialty { get; set; }
    public required int TeacherId { get; set; }
    public required Teacher Teacher { get; set; }

    private Group()
    {
    }

    public Group(string name, string specialty, int teacherId)
    {
        Name = name;
        Specialty = specialty;
        TeacherId = teacherId;
    }

    public Group(int id, string name, string specialty, Teacher teacher)
    {
        Id = id;
        Name = name;
        Specialty = specialty;
        Teacher = teacher;
    }
}
