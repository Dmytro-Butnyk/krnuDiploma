using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.ArchiveGroup;

public sealed class Defence : BaseEntity
{
    public DateOnly? DefenceDate { get; set; }
    public int QueueNumber { get; set; }
    public string ProtocolNumber { get; set; }

    // 1-to-1 with QualificationWork
    public int QualificationWorkId { get; set; }
    public QualificationWork? QualificationWork { get; set; }

    // N-to-1 with DiplomaExaminationCommission
    public int? DiplomaExaminationCommissionId { get; set; }
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; set; }

    private Defence()
    {
        ProtocolNumber = string.Empty;
    }

    public Defence(DateOnly? defenceDate, int queueNumber, string protocolNumber, int qualificationWorkId, int? diplomaExaminationCommissionId)
    {
        DefenceDate = defenceDate;
        QueueNumber = queueNumber;
        ProtocolNumber = protocolNumber;
        QualificationWorkId = qualificationWorkId;
        DiplomaExaminationCommissionId = diplomaExaminationCommissionId;
    }

    public static Defence CreateDraft(int qualificationWorkId)
    {
        return new Defence(
            defenceDate: null,
            queueNumber: 0,
            protocolNumber: string.Empty,
            qualificationWorkId: qualificationWorkId,
            diplomaExaminationCommissionId: null);
    }
}
