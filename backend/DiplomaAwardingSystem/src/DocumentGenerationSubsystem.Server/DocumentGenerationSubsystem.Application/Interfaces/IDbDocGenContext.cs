using Core.Domain.Entities;
using DocumentGenerationSubsystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Application.Interfaces;

public interface IDbDocGenContext
{
    DbSet<Group> Groups { get; }
    DbSet<QualificationWork> QualificationWorks { get; }
    DbSet<Rector> Rectors { get; }
    DbSet<Student> Students { get; }
    DbSet<Teacher> Teachers { get; }
    DbSet<DocumentTemplate> DocumentTemplates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
