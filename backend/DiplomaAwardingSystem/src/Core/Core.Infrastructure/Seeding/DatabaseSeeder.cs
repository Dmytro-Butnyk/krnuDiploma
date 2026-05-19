using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(DbDocGenContext context)
    {
        // 1. Fail-Fast check: If there are any departments, assume DB is already seeded.
        if (await context.Departments.AnyAsync())
        {
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // --- 1. Independent Dictionaries ---
            var department = new Department("Department of Computer Science and Software Engineering");
            context.Departments.Add(department);
            
            var degreeProf = new AcademicDegree("Doctor of Sciences", "Dr.Sc.");
            var degreeAssoc = new AcademicDegree("Candidate of Sciences", "Ph.D.");
            context.AcademicDegrees.AddRange(degreeProf, degreeAssoc);
            
            await context.SaveChangesAsync(); // IDs are generated here

            // --- 2. Level 1 Dependencies (Specialties & Teachers) ---
            var specialty = new Specialty("121", "Software Engineering", department.Id);
            context.Specialties.Add(specialty);

            var teacherHead = new Teacher("Dr. Alan Turing", "Turing A.", "alan@turing.edu", "555-0101", "Professor", degreeProf.Id, department.Id);
            var teacherMember = new Teacher("Dr. Ada Lovelace", "Lovelace A.", "ada@lovelace.edu", "555-0102", "Associate Professor", degreeAssoc.Id, department.Id);
            var teacherSecretary = new Teacher("Grace Hopper", "Hopper G.", "grace@hopper.edu", "555-0103", "Senior Lecturer", degreeAssoc.Id, department.Id);
            context.Teachers.AddRange(teacherHead, teacherMember, teacherSecretary);

            await context.SaveChangesAsync();

            // --- 3. Level 2 Dependencies (Groups & DecMembers profiles) ---
            var group = new Group("SE-41", "2026", EducationLevel.Bachelor, specialty.Id); // Assuming EducationLevel.Bachelor exists
            context.Groups.Add(group);

            var headProfile = new DecMember(CommissionRole.Head, teacherHead.Id); // Assuming CommissionRole.Head exists
            var memberProfile = new DecMember(CommissionRole.Member, teacherMember.Id);
            var secretaryProfile = new DecMember(CommissionRole.Secretary, teacherSecretary.Id);
            context.DecMembers.AddRange(headProfile, memberProfile, secretaryProfile);

            await context.SaveChangesAsync();

            // --- 4. Level 3 Dependencies (Students & DEC) ---
            var student1 = new Student("John Doe", group.Id);
            var student2 = new Student("Jane Smith", group.Id);
            context.Students.AddRange(student1, student2);

            // 1-to-1 relationship: One DEC for this exact Group
            var dec = new DiplomaExaminationCommission(
                orderNumber: 101, 
                startDate: new DateOnly(2026, 6, 1), 
                endDate: new DateOnly(2026, 6, 30), 
                groupId: group.Id);
            context.DiplomaExaminationCommissions.Add(dec);

            await context.SaveChangesAsync();

            // --- 5. Level 4 Dependencies (DecToMember Many-to-Many & Qualification Works) ---
            
            // Linking profiles to the specific Commission
            var decToHead = new DecToMember(headProfile.Id, dec.Id);
            var decToMember = new DecToMember(memberProfile.Id, dec.Id);
            var decToSecretary = new DecToMember(secretaryProfile.Id, dec.Id);
            context.DecToMembers.AddRange(decToHead, decToMember, decToSecretary);

            // 1-to-1: Student -> QualificationWork
            // N-to-1: Teacher -> QualificationWork
            var qw1 = new QualificationWork(
                topic: "Microservices Architecture in .NET", pagesCount: 85, plagiarismPercent: 2.5f, uniquePercent: 97.5f,
                supervisorScore: 95, reviewerScore: 92, commissionScore: 94,
                ectsGrade: EctsGrade.A, nationalGrade: NationalGrade.Excellent, // Assuming these enums exist
                practiceBase: string.Empty, hasDiplomaWithHonors: false,
                studentId: student1.Id, teacherId: teacherHead.Id, reviewerId: null);

            var qw2 = new QualificationWork(
                topic: "AI-driven Data Generation", pagesCount: 78, plagiarismPercent: 5.0f, uniquePercent: 95.0f,
                supervisorScore: 85, reviewerScore: 88, commissionScore: 87,
                ectsGrade: EctsGrade.B, nationalGrade: NationalGrade.Good,
                practiceBase: string.Empty, hasDiplomaWithHonors: false,
                studentId: student2.Id, teacherId: teacherMember.Id, reviewerId: null);
                
            context.QualificationWorks.AddRange(qw1, qw2);

            await context.SaveChangesAsync();

            // --- 6. Level 5 Dependencies (Defences & Archives) ---
            
            // 1-to-1: QW -> Defence
            // N-to-1: DEC -> Defence
            var defence1 = new Defence(new DateOnly(2026, 6, 15), 1, "PR-01", qw1.Id, dec.Id);
            var defence2 = new Defence(new DateOnly(2026, 6, 15), 2, "PR-02", qw2.Id, dec.Id);
            context.Defences.AddRange(defence1, defence2);

            // 1-to-1: DEC -> Archive
            var archive = new Archive("PR-01 - PR-10", "CASE-2026-SE", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), 500, dec.Id);
            context.Archives.Add(archive);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw; // Fail-fast: let the application crash on startup if seeding fails
        }
    }
}
