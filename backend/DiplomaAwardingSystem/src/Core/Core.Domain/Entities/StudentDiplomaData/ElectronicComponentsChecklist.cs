using Core.Domain.Entities.StudyGroup;

namespace Core.Domain.Entities.StudentDiplomaData;

public sealed class ElectronicComponentsChecklist : BaseEntity
{
    public bool HasRegulatoryControl { get; set; }
    public bool HasExplanatoryNoteDoc { get; set; }
    public bool HasExplanatoryNotePdf { get; set; }
    public bool HasPlagiarismReportPdf { get; set; }
    public bool HasReviewDoc { get; set; }
    public bool HasPresentationPpt { get; set; }

    // 1-to-1 with Student
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    private ElectronicComponentsChecklist() { }

    public ElectronicComponentsChecklist(
        bool hasRegulatoryControl,
        bool hasExplanatoryNoteDoc,
        bool hasExplanatoryNotePdf,
        bool hasPlagiarismReportPdf,
        bool hasReviewDoc,
        bool hasPresentationPpt,
        int studentId)
    {
        HasRegulatoryControl = hasRegulatoryControl;
        HasExplanatoryNoteDoc = hasExplanatoryNoteDoc;
        HasExplanatoryNotePdf = hasExplanatoryNotePdf;
        HasPlagiarismReportPdf = hasPlagiarismReportPdf;
        HasReviewDoc = hasReviewDoc;
        HasPresentationPpt = hasPresentationPpt;
        StudentId = studentId;
    }

    public static ElectronicComponentsChecklist CreateEmpty(int studentId)
    {
        return new ElectronicComponentsChecklist(
            hasRegulatoryControl: false,
            hasExplanatoryNoteDoc: false,
            hasExplanatoryNotePdf: false,
            hasPlagiarismReportPdf: false,
            hasReviewDoc: false,
            hasPresentationPpt: false,
            studentId: studentId);
    }
}
