namespace Core.Domain.Entities.StudyGroup;

public sealed class Secretary : BaseEntity
{
    public string Email { get; set; }
    public string? GoogleSubject { get; set; }
    public string FullName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuperSecretary { get; set; }

    // N-to-1 with Specialty
    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    private Secretary()
    {
        Email = string.Empty;
        FullName = string.Empty;
    }

    public Secretary(
        string email,
        string fullName,
        int specialtyId,
        bool isActive = true,
        bool isSuperSecretary = false)
    {
        Email = email;
        FullName = fullName;
        SpecialtyId = specialtyId;
        IsActive = isActive;
        IsSuperSecretary = isSuperSecretary;
    }
}
