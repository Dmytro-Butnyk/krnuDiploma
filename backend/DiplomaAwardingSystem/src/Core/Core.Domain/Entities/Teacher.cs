namespace Core.Domain.Entities;

public sealed class Teacher : BaseEntity
{
    public required string FullName { get; init; }
    public required string Position { get; init; }
    
    private Teacher()
    {
    }
    
    public Teacher(string fullName, string position)
    {
        FullName = fullName;
        Position = position;
    }
}
