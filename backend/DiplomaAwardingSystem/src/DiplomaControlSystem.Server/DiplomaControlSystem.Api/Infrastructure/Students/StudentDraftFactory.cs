using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Core.Domain.Entities.StudyGroup;

namespace DiplomaControlSystem.Api.Infrastructure.Students;

internal static class StudentDraftFactory
{
    public static Student Create(string fullName)
    {
        var student = new Student(fullName, groupId: 0);
        student.ElectronicComponentsChecklist = ElectronicComponentsChecklist.CreateEmpty(studentId: 0);
        student.PhysicalComponentsChecklist = PhysicalComponentsChecklist.CreateEmpty(studentId: 0);

        var qualificationWork = QualificationWork.CreateDraft(studentId: 0);
        qualificationWork.QualificationWorkCharacteristics = QualificationWorkCharacteristics.CreateEmpty(qualificationWorkId: 0);
        student.QualificationWork = qualificationWork;

        return student;
    }
}
