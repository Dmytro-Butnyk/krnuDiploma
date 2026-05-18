namespace Core.Domain.Entities.StudyGroup;

public sealed class Secretary : BaseEntity
{
    public string Email { get; set; }
    public string FullName { get; set; }
    public bool IsActive { get; set; }

    // N-to-1 with Specialty
    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    private Secretary()
    {
        Email = string.Empty;
        FullName = string.Empty;
    }

    public Secretary(string email, string fullName, int specialtyId, bool isActive = true)
    {
        Email = email;
        FullName = fullName;
        SpecialtyId = specialtyId;
        IsActive = isActive;
    }
}
