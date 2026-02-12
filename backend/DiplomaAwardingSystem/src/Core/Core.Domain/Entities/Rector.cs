namespace Core.Domain.Entities;

public sealed class Rector : BaseEntity
{
    public required string FullName { get; set; }
    public required bool IsActive { get; set; }
    
    private Rector()
    {
    }
    
    public Rector(string fullName, bool isActive)
    {
        FullName = fullName;
        IsActive = isActive;
    }
}
