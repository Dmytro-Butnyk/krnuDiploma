namespace Core.Domain.Entities;

public sealed class Teacher : BaseEntity
{
    public required string FullName { get; init; }
    public required string Position { get; init; }
    
    public Group? CuratedGroup { get; init; }
    public ICollection<QualificationWork> SupervisedWorks { get; init; } = new List<QualificationWork>();

    private Teacher()
    {
    }
    
    public Teacher(string fullName, string position)
    {
        FullName = fullName;
        Position = position;
    }
}
