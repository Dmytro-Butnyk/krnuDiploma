using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.ArchiveGroup;

public sealed class Archive : BaseEntity
{
    public string ProtocolRange { get; init; }
    public string CaseNumber { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TotalPages { get; init; }
    
    // 1-to-1 with DiplomaExaminationCommission
    public int DiplomaExaminationCommissionId { get; init; }
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; init; }

    private Archive()
    {
        ProtocolRange = string.Empty;
        CaseNumber = string.Empty;
    }

    public Archive(string protocolRange, string caseNumber, DateOnly startDate, DateOnly endDate, int totalPages, int diplomaExaminationCommissionId)
    {
        ProtocolRange = protocolRange;
        CaseNumber = caseNumber;
        StartDate = startDate;
        EndDate = endDate;
        TotalPages = totalPages;
        DiplomaExaminationCommissionId = diplomaExaminationCommissionId;
    }
}
