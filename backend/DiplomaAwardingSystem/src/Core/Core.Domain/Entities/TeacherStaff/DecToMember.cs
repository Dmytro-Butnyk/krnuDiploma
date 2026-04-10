namespace Core.Domain.Entities.TeacherStaff;

public sealed class DecToMember : BaseEntity
{
    public int DecMemberId { get; init; }
    public DecMember? DecMember { get; init; }
    
    public int DiplomaExaminationCommissionId { get; init; }
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; init; }

    private DecToMember() { }

    public DecToMember(int decMemberId, int diplomaExaminationCommissionId)
    {
        DecMemberId = decMemberId;
        DiplomaExaminationCommissionId = diplomaExaminationCommissionId;
    }
}
