using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.StudentDiplomaData;

public sealed class PhysicalComponentsChecklist : BaseEntity
{
    public bool HasStudentCard { get; set; }
    public bool HasGradeBook { get; set; }
    public bool HasCircular { get; set; }
    public bool HasSignedReview { get; set; }
    public bool HasCopyOfBankReceipt { get; set; }
    public bool HasExplanatoryNote { get; set; }

    // 1-to-1 with Student
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    private PhysicalComponentsChecklist() { }

    public PhysicalComponentsChecklist(
        bool hasStudentCard,
        bool hasGradeBook,
        bool hasCircular,
        bool hasSignedReview,
        bool hasCopyOfBankReceipt,
        bool hasExplanatoryNote,
        int studentId)
    {
        HasStudentCard = hasStudentCard;
        HasGradeBook = hasGradeBook;
        HasCircular = hasCircular;
        HasSignedReview = hasSignedReview;
        HasCopyOfBankReceipt = hasCopyOfBankReceipt;
        HasExplanatoryNote = hasExplanatoryNote;
        StudentId = studentId;
    }

    public static PhysicalComponentsChecklist CreateEmpty(int studentId)
    {
        return new PhysicalComponentsChecklist(
            hasStudentCard: false,
            hasGradeBook: false,
            hasCircular: false,
            hasSignedReview: false,
            hasCopyOfBankReceipt: false,
            hasExplanatoryNote: false,
            studentId: studentId);
    }
}
