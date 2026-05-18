namespace Core.Domain.Entities.TeacherStaff;

public sealed class DecToMember : BaseEntity
{
    public int DecMemberId { get; set; }
    public DecMember? DecMember { get; set; }
    
    public int DiplomaExaminationCommissionId { get; set; }
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; set; }

    private DecToMember() { }

    public DecToMember(int decMemberId, int diplomaExaminationCommissionId)
    {
        DecMemberId = decMemberId;
        DiplomaExaminationCommissionId = diplomaExaminationCommissionId;
    }
}
