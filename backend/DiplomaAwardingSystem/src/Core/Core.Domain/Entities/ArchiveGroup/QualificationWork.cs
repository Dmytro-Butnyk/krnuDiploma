using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.Enums;

namespace Core.Domain.Entities.ArchiveGroup;

public sealed class QualificationWork : BaseEntity
{
    public string Topic { get; init; }
    public int PagesCount { get; init; }
    public float PlagiarismPercent { get; init; }
    public float UniquePercent { get; init; }
    public int SupervisorScore { get; init; }
    public int ReviewerScore { get; init; }
    public int CommissionScore { get; init; }
    public EctsGrade EctsGrade { get; init; }
    public NationalGrade NationalGrade { get; init; }

    // 1-to-1 with Student
    public int StudentId { get; init; }
    public Student? Student { get; init; }

    // N-to-1 with Teacher
    public int TeacherId { get; init; }
    public Teacher? Teacher { get; init; }

    // 1-to-1 with Defence
    public Defence? Defence { get; init; }

    private QualificationWork()
    {
        Topic = string.Empty;
    }

    public QualificationWork(string topic, int pagesCount, float plagiarismPercent, float uniquePercent, int supervisorScore, int reviewerScore, int commissionScore, EctsGrade ectsGrade, NationalGrade nationalGrade, int studentId, int teacherId)
    {
        Topic = topic;
        PagesCount = pagesCount;
        PlagiarismPercent = plagiarismPercent;
        UniquePercent = uniquePercent;
        SupervisorScore = supervisorScore;
        ReviewerScore = reviewerScore;
        CommissionScore = commissionScore;
        EctsGrade = ectsGrade;
        NationalGrade = nationalGrade;
        StudentId = studentId;
        TeacherId = teacherId;
    }
}
