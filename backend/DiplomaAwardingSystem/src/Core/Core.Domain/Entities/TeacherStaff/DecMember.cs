using Core.Domain.Enums;

namespace Core.Domain.Entities.TeacherStaff;

public sealed class DecMember : BaseEntity
{
    public CommissionRole Role { get; init; }
    public int DiplomaExaminationCommissionId { get; init; }
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; init; }
    public int TeacherId { get; init; }
    public Teacher? Teacher { get; init; }

    private DecMember()
    {
    }
    
    public DecMember(CommissionRole role, int diplomaExaminationCommissionId, int teacherId)
    {
        Role = role;
        DiplomaExaminationCommissionId = diplomaExaminationCommissionId;
        TeacherId = teacherId;
    }
}
