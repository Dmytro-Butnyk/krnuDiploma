using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Infrastructure.Engines;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Security;

public static class DocumentGenerationAllowedEntities
{
    public static readonly IReadOnlyDictionary<string, Func<DbDocGenContext, IReadOnlyCollection<string>?, IQueryable>> Registry = 
        new Dictionary<string, Func<DbDocGenContext, IReadOnlyCollection<string>?, IQueryable>>(StringComparer.OrdinalIgnoreCase)
        {
            // Archive group
            { "Archive", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Archives, inc) },
            { "QualificationWork", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.QualificationWorks, inc) },
            
            // Study group
            { "Department", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Departments, inc) },
            { "Group", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Groups, inc) },
            { "Specialty", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Specialties, inc) },
            { "Student", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Students, inc) },
            
            // Teacher staff
            { "AcademicDegree", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.AcademicDegrees, inc) },
            { "DiplomaExaminationCommission", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.DiplomaExaminationCommissions, inc) },
            { "Teacher", (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Teachers, inc) }
        };
}
