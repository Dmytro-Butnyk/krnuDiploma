using System.Reflection;
using Core.Domain;
using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure;

public sealed class DbDocGenContext(
    DbContextOptions<DbDocGenContext> options,
    IEnumerable<IEntityConfigurationMarker> markers) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("diploma");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        if (markers != null)
        {
            foreach (var marker in markers)
            {
                modelBuilder.ApplyConfigurationsFromAssembly(marker.GetType().Assembly);
            }
        }
    }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     if (optionsBuilder.IsConfigured) return;
    //     
    //     optionsBuilder.UseNpgsql("");
    // }

    // Archive group
    public DbSet<Archive> Archives => Set<Archive>();
    public DbSet<QualificationWork> QualificationWorks => Set<QualificationWork>();

    // Study group
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Secretary> Secretaries => Set<Secretary>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Student> Students => Set<Student>();

    // Student diploma data
    public DbSet<ElectronicComponentsChecklist> ElectronicComponentsChecklists => Set<ElectronicComponentsChecklist>();
    public DbSet<PhysicalComponentsChecklist> PhysicalComponentsChecklists => Set<PhysicalComponentsChecklist>();
    public DbSet<QualificationWorkCharacteristics> QualificationWorkCharacteristics => Set<QualificationWorkCharacteristics>();

    // Teacher staff
    public DbSet<AcademicDegree> AcademicDegrees => Set<AcademicDegree>();
    public DbSet<DiplomaExaminationCommission> DiplomaExaminationCommissions => Set<DiplomaExaminationCommission>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
}
