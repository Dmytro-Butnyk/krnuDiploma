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
        Func<DbDocGenContext, IReadOnlyCollection<string>?, IQueryable> QueryFactory,
        IReadOnlySet<string> ProtectedProperties)
    {
        public EntityRegistration(
            Type clrType,
            Func<DbDocGenContext, IReadOnlyCollection<string>?, IQueryable> queryFactory)
            : this(
                clrType,
                queryFactory,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public EntityRegistration(
            Type clrType,
            Func<DbDocGenContext, IReadOnlyCollection<string>?, IQueryable> queryFactory,
            params string[] protectedProperties)
            : this(
                clrType,
                queryFactory,
                protectedProperties.ToHashSet(StringComparer.OrdinalIgnoreCase))
        {
        }

        public bool AllowsProperty(string propertyName) => !ProtectedProperties.Contains(propertyName);
    }

    public static readonly IReadOnlyDictionary<string, EntityRegistration> Registry =
        new Dictionary<string, EntityRegistration>(StringComparer.OrdinalIgnoreCase)
        {
            // Archive group
            { nameof(Archive), new EntityRegistration(typeof(Archive), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Archives, inc)) },
            { nameof(QualificationWork), new EntityRegistration(typeof(QualificationWork), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.QualificationWorks, inc)) },
            
            // Study group
            { nameof(Group), new EntityRegistration(typeof(Group), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Groups, inc)) },
            { 
                nameof(Secretary), new EntityRegistration(typeof(Secretary), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Secretaries, inc),
                nameof(Secretary.GoogleSubject),
                nameof(Secretary.IsSuperSecretary),
                nameof(Secretary.IsActive)
                )
            },
            { nameof(Specialty), new EntityRegistration(typeof(Specialty), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Specialties, inc)) },
            { nameof(Student), new EntityRegistration(typeof(Student), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Students, inc)) },

            // Student diploma data
            { nameof(ElectronicComponentsChecklist), new EntityRegistration(typeof(ElectronicComponentsChecklist), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.ElectronicComponentsChecklists, inc)) },
            { nameof(PhysicalComponentsChecklist), new EntityRegistration(typeof(PhysicalComponentsChecklist), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.PhysicalComponentsChecklists, inc)) },
            { nameof(QualificationWorkCharacteristics), new EntityRegistration(typeof(QualificationWorkCharacteristics), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.QualificationWorkCharacteristics, inc)) },
            
            // Teacher staff
            { nameof(AcademicDegree), new EntityRegistration(typeof(AcademicDegree), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.AcademicDegrees, inc)) },
            { nameof(CommissionHead), new EntityRegistration(typeof(CommissionHead), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.CommissionHeads, inc)) },
            { nameof(DiplomaExaminationCommission), new EntityRegistration(typeof(DiplomaExaminationCommission), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.DiplomaExaminationCommissions, inc)) },
            { nameof(Teacher), new EntityRegistration(typeof(Teacher), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.Teachers, inc)) },
            { nameof(TeacherPosition), new EntityRegistration(typeof(TeacherPosition), (ctx, inc) => DynamicDocumentEngine.BuildQuery(ctx.TeacherPositions, inc)) }
        };

    public static bool ContainsProtectedPropertyReference(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        foreach (var protectedProperty in Registry.Values.SelectMany(registration => registration.ProtectedProperties).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (ContainsIdentifier(expression, protectedProperty))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsProtectedPropertyReference(IReadOnlyCollection<string>? expressions)
    {
        return expressions is not null && expressions.Any(ContainsProtectedPropertyReference);
    }

    private static bool ContainsIdentifier(string expression, string identifier)
    {
        var startIndex = 0;

        while (startIndex < expression.Length)
        {
            var index = expression.IndexOf(identifier, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var beforeIsIdentifier = index > 0 && IsIdentifierCharacter(expression[index - 1]);
            var afterIndex = index + identifier.Length;
            var afterIsIdentifier = afterIndex < expression.Length && IsIdentifierCharacter(expression[afterIndex]);

            if (!beforeIsIdentifier && !afterIsIdentifier)
            {
                return true;
            }

            startIndex = index + identifier.Length;
        }

        return false;
    }

    private static bool IsIdentifierCharacter(char value)
    {
        return char.IsAsciiLetterOrDigit(value) || value == '_';
    }
}
