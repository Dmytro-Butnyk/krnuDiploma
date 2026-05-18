using Core.Domain.Enums;

namespace Core.Domain.Entities.TeacherStaff;

// Сущность-профиль члена комиссии
public sealed class DecMember : BaseEntity
{
    public CommissionRole Role { get; set; }
    
    // N-to-1 with Teacher
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    // Navigation property for Many-to-Many join table
    public ICollection<DecToMember> DecToMembers { get; init; } = new HashSet<DecToMember>();

    private DecMember() { }
    
    public DecMember(CommissionRole role, int teacherId)
    {
        Role = role;
        TeacherId = teacherId;
    }
}
