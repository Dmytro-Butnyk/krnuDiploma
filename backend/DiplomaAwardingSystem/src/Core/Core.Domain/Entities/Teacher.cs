namespace Core.Domain.Entities;

public sealed class Teacher : BaseEntity
{
    public required string FullName { get; set; }
    public required string Position { get; set; }
    
    private Teacher()
    {
    }
    
    public Teacher(string fullName, string position)
    {
        FullName = fullName;
        Position = position;
    }
}
