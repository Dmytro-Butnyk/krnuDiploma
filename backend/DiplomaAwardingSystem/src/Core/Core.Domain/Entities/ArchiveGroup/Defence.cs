using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.ArchiveGroup;

public sealed class Defence : BaseEntity
{
    public DateOnly DefenceDate { get; init; }
    public int QueueNumber { get; init; }
    public string ProtocolNumber { get; init; }

    // 1-to-1 with QualificationWork
    public int QualificationWorkId { get; init; }
    public QualificationWork? QualificationWork { get; init; }

    // N-to-1 with DiplomaExaminationCommission
    public int DiplomaExaminationCommissionId { get; init; }
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; init; }

    private Defence()
    {
        ProtocolNumber = string.Empty;
    }

    public Defence(DateOnly defenceDate, int queueNumber, string protocolNumber, int qualificationWorkId, int diplomaExaminationCommissionId)
    {
        DefenceDate = defenceDate;
        QueueNumber = queueNumber;
        ProtocolNumber = protocolNumber;
        QualificationWorkId = qualificationWorkId;
        DiplomaExaminationCommissionId = diplomaExaminationCommissionId;
    }
}
