using Core.Domain.Entities.TeacherStaff;

namespace Core.Domain.Entities.ArchiveGroup;

public sealed class Archive : BaseEntity
{
    public string ProtocolRange { get; set; }
    public string CaseNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalPages { get; set; }
    
    // 1-to-1 with DiplomaExaminationCommission
    public int DiplomaExaminationCommissionId { get; set; }
    public DiplomaExaminationCommission? DiplomaExaminationCommission { get; set; }

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
