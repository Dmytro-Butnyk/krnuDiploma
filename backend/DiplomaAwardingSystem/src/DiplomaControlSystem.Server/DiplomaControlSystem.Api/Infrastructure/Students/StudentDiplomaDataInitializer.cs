using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Core.Domain.Entities.StudyGroup;

namespace DiplomaControlSystem.Api.Infrastructure.Students;

internal static class StudentDiplomaDataInitializer
{
    public static QualificationWork EnsureQualificationWork(Student student)
    {
        if (student.QualificationWork is not null)
        {
            return student.QualificationWork;
        }

        student.QualificationWork = QualificationWork.CreateDraft(student.Id);
        return student.QualificationWork;
    }

    public static Defence EnsureDefence(QualificationWork qualificationWork)
    {
        if (qualificationWork.Defence is not null)
        {
            return qualificationWork.Defence;
        }

        qualificationWork.Defence = Defence.CreateDraft(qualificationWork.Id);
        return qualificationWork.Defence;
    }

    public static QualificationWorkCharacteristics EnsureCharacteristics(QualificationWork qualificationWork)
    {
        if (qualificationWork.QualificationWorkCharacteristics is not null)
        {
            return qualificationWork.QualificationWorkCharacteristics;
        }

        qualificationWork.QualificationWorkCharacteristics = QualificationWorkCharacteristics.CreateEmpty(qualificationWork.Id);
        return qualificationWork.QualificationWorkCharacteristics;
    }

    public static PhysicalComponentsChecklist EnsurePhysicalChecklist(Student student)
    {
        if (student.PhysicalComponentsChecklist is not null)
        {
            return student.PhysicalComponentsChecklist;
        }

        student.PhysicalComponentsChecklist = PhysicalComponentsChecklist.CreateEmpty(student.Id);
        return student.PhysicalComponentsChecklist;
    }

    public static ElectronicComponentsChecklist EnsureElectronicChecklist(Student student)
    {
        if (student.ElectronicComponentsChecklist is not null)
        {
            return student.ElectronicComponentsChecklist;
        }

        student.ElectronicComponentsChecklist = ElectronicComponentsChecklist.CreateEmpty(student.Id);
        return student.ElectronicComponentsChecklist;
    }
}
