using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.Enums;

namespace Core.Domain.Entities.ArchiveGroup;

public sealed class QualificationWork : BaseEntity
{
    public string Topic { get; set; }
    public int PagesCount { get; set; }
    public float PlagiarismPercent { get; set; }
    public float UniquePercent { get; set; }
    public int SupervisorScore { get; set; }
    public int ReviewerScore { get; set; }
    public int CommissionScore { get; set; }
    public EctsGrade EctsGrade { get; set; }
    public NationalGrade NationalGrade { get; set; }
    public string PracticeBase { get; set; }
    public bool HasDiplomaWithHonors { get; set; }

    // 1-to-1 with Student
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    // N-to-1 with Teacher
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    // N-to-1 with Teacher
    public int? ReviewerId { get; set; }
    public Teacher? Reviewer { get; set; }

    // 1-to-1 with Defence
    public Defence? Defence { get; set; }

    // 1-to-1 with QualificationWorkCharacteristics
    public QualificationWorkCharacteristics? QualificationWorkCharacteristics { get; set; }

    private QualificationWork()
    {
        Topic = string.Empty;
        PracticeBase = string.Empty;
    }

    public QualificationWork(string topic, int pagesCount, float plagiarismPercent, float uniquePercent, int supervisorScore, int reviewerScore, int commissionScore, EctsGrade ectsGrade, NationalGrade nationalGrade, string practiceBase, bool hasDiplomaWithHonors, int studentId, int? teacherId, int? reviewerId)
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
        PracticeBase = practiceBase;
        HasDiplomaWithHonors = hasDiplomaWithHonors;
        StudentId = studentId;
        TeacherId = teacherId;
        ReviewerId = reviewerId;
    }

    public static QualificationWork CreateDraft(int studentId)
    {
        return new QualificationWork(
            topic: string.Empty,
            pagesCount: 0,
            plagiarismPercent: 0,
            uniquePercent: 0,
            supervisorScore: 0,
            reviewerScore: 0,
            commissionScore: 0,
            ectsGrade: EctsGrade.None,
            nationalGrade: NationalGrade.None,
            practiceBase: string.Empty,
            hasDiplomaWithHonors: false,
            studentId: studentId,
            teacherId: null,
            reviewerId: null);
    }
}
