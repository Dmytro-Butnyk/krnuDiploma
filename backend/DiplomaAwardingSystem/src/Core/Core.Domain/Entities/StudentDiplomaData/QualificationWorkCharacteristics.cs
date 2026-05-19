using Core.Domain.Entities.ArchiveGroup;

namespace Core.Domain.Entities.StudentDiplomaData;

public sealed class QualificationWorkCharacteristics : BaseEntity
{
    public bool IsResearchBased { get; set; }
    public bool HasRealProjects { get; set; }
    public bool IsEcoFriendly { get; set; }
    public bool IsEnterpriseOrdered { get; set; }
    public bool IsComplexInteruniversity { get; set; }
    public bool IsComplexInterdepartmental { get; set; }
    public bool IsComplexDepartmental { get; set; }
    public bool IsComplexProjectParticipant { get; set; }
    public bool IsRecommendedForMaster { get; set; }
    public bool IsRecommendedForImplementation { get; set; }
    public bool IsDefendedAtEnterprise { get; set; }

    // 1-to-1 with QualificationWork
    public int QualificationWorkId { get; set; }
    public QualificationWork? QualificationWork { get; set; }

    private QualificationWorkCharacteristics() { }

    public QualificationWorkCharacteristics(
        bool isResearchBased,
        bool hasRealProjects,
        bool isEcoFriendly,
        bool isEnterpriseOrdered,
        bool isComplexInteruniversity,
        bool isComplexInterdepartmental,
        bool isComplexDepartmental,
        bool isComplexProjectParticipant,
        bool isRecommendedForMaster,
        bool isRecommendedForImplementation,
        bool isDefendedAtEnterprise,
        int qualificationWorkId)
    {
        IsResearchBased = isResearchBased;
        HasRealProjects = hasRealProjects;
        IsEcoFriendly = isEcoFriendly;
        IsEnterpriseOrdered = isEnterpriseOrdered;
        IsComplexInteruniversity = isComplexInteruniversity;
        IsComplexInterdepartmental = isComplexInterdepartmental;
        IsComplexDepartmental = isComplexDepartmental;
        IsComplexProjectParticipant = isComplexProjectParticipant;
        IsRecommendedForMaster = isRecommendedForMaster;
        IsRecommendedForImplementation = isRecommendedForImplementation;
        IsDefendedAtEnterprise = isDefendedAtEnterprise;
        QualificationWorkId = qualificationWorkId;
    }

    public static QualificationWorkCharacteristics CreateEmpty(int qualificationWorkId)
    {
        return new QualificationWorkCharacteristics(
            isResearchBased: false,
            hasRealProjects: false,
            isEcoFriendly: false,
            isEnterpriseOrdered: false,
            isComplexInteruniversity: false,
            isComplexInterdepartmental: false,
            isComplexDepartmental: false,
            isComplexProjectParticipant: false,
            isRecommendedForMaster: false,
            isRecommendedForImplementation: false,
            isDefendedAtEnterprise: false,
            qualificationWorkId: qualificationWorkId);
    }
}
