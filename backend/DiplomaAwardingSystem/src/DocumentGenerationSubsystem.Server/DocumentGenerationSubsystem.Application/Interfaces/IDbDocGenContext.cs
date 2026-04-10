using Core.Domain.Entities;
using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.TeacherStaff;
using DocumentGenerationSubsystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Application.Interfaces;

public interface IDbDocGenContext
{
    // Archive group
    DbSet<Archive> Archives { get; }
    DbSet<Defence> Defences { get; }
    DbSet<QualificationWork> QualificationWorks { get; }
    
    // Study group
    DbSet<Department> Departments { get; }
    DbSet<Group> Groups { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<Student> Students { get; }
    
    // Teacher staff
    DbSet<AcademicDegree> AcademicDegrees { get; }
    DbSet<DecMember> DecMembers { get; }
    DbSet<DecToMember> DecToMembers { get; }
    DbSet<DiplomaExaminationCommission> DiplomaExaminationCommissions { get; }
    DbSet<Teacher> Teachers { get; }
    
    // Document generator
    DbSet<DocumentTemplate> DocumentTemplates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
