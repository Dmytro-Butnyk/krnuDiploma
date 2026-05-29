using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.TeacherStaff;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Infrastructure.Engines;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Security;

public static class DocumentGenerationAllowedEntities
{
    public sealed record EntityRegistration(
        Type ClrType,
        Func<DbDocGenContext, IReadOnlyCollection<string>?, IQueryable> QueryFactory);

    public static readonly IReadOnlyDictionary<string, EntityRegistration> Registry =
        new Dictionary<string, EntityRegistration>(StringComparer.OrdinalIgnoreCase)
        {
            // Archive group
            { "Archive", new EntityRegistration(typeof(Archive), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Archives, inc)) },
            { "QualificationWork", new EntityRegistration(typeof(QualificationWork), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.QualificationWorks, inc)) },
            
            // Study group
            { "Group", new EntityRegistration(typeof(Group), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Groups, inc)) },
            { "Secretary", new EntityRegistration(typeof(Secretary), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Secretaries, inc)) },
            { "Specialty", new EntityRegistration(typeof(Specialty), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Specialties, inc)) },
            { "Student", new EntityRegistration(typeof(Student), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Students, inc)) },

            // Student diploma data
            { "ElectronicComponentsChecklist", new EntityRegistration(typeof(ElectronicComponentsChecklist), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.ElectronicComponentsChecklists, inc)) },
            { "PhysicalComponentsChecklist", new EntityRegistration(typeof(PhysicalComponentsChecklist), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.PhysicalComponentsChecklists, inc)) },
            { "QualificationWorkCharacteristics", new EntityRegistration(typeof(QualificationWorkCharacteristics), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.QualificationWorkCharacteristics, inc)) },
            
            // Teacher staff
            { "AcademicDegree", new EntityRegistration(typeof(AcademicDegree), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.AcademicDegrees, inc)) },
            { "CommissionHead", new EntityRegistration(typeof(CommissionHead), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.CommissionHeads, inc)) },
            { "DiplomaExaminationCommission", new EntityRegistration(typeof(DiplomaExaminationCommission), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.DiplomaExaminationCommissions, inc)) },
            { "Teacher", new EntityRegistration(typeof(Teacher), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Teachers, inc)) },
            { "TeacherPosition", new EntityRegistration(typeof(TeacherPosition), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.TeacherPositions, inc)) }
        };
}
