using System.Reflection;
using Core.Domain.Entities;
using DocumentGenerationSubsystem.Application.Interfaces;
using DocumentGenerationSubsystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Infrastructure;

public sealed class DbDocGenContext(DbContextOptions<DbDocGenContext> options) : DbContext(options), IDbDocGenContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("diploma");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     if (optionsBuilder.IsConfigured) return;
    //     
    //     optionsBuilder.UseNpgsql("");
    // }

    public DbSet<Group> Groups => Set<Group>();
    public DbSet<QualificationWork> QualificationWorks => Set<QualificationWork>();
    public DbSet<Rector> Rectors => Set<Rector>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
}
