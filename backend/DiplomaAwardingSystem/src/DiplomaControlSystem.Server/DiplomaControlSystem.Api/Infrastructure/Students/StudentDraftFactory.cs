using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Core.Domain.Entities.StudyGroup;

namespace DiplomaControlSystem.Api.Infrastructure.Students;

internal static class StudentDraftFactory
{
    public static Student Create(string fullName)
    {
        return Create(fullName, topic: string.Empty, practiceBase: string.Empty, teacherId: null);
    }

    public static Student Create(string fullName, string topic, string practiceBase, int? teacherId)
    {
        var student = new Student(fullName, groupId: 0);
        student.ElectronicComponentsChecklist = ElectronicComponentsChecklist.CreateEmpty(studentId: 0);
        student.PhysicalComponentsChecklist = PhysicalComponentsChecklist.CreateEmpty(studentId: 0);

        var qualificationWork = QualificationWork.CreateDraft(studentId: 0);
        qualificationWork.Topic = topic.Trim();
        qualificationWork.PracticeBase = practiceBase.Trim();
        qualificationWork.TeacherId = teacherId;
        qualificationWork.QualificationWorkCharacteristics = QualificationWorkCharacteristics.CreateEmpty(qualificationWorkId: 0);
        student.QualificationWork = qualificationWork;

        return student;
    }
}
