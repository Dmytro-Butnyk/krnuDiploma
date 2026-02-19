namespace Core.Domain.Entities;

public sealed class Rector : BaseEntity
{
    public required string FullName { get; init; }
    public required bool IsActive { get; init; }
    
    private Rector()
    {
    }
    
    public Rector(string fullName, bool isActive)
    {
        FullName = fullName;
        IsActive = isActive;
    }
}
