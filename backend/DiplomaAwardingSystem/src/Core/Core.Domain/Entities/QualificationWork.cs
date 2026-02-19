namespace Core.Domain.Entities;

public sealed class QualificationWork : BaseEntity
{
    public required string Name { get; init; }
    public required int StudentId { get; init; }
    public required Student Student { get; init; }
    public required int TeacherId { get; init; }
    public required Teacher Teacher { get; init; }

    private QualificationWork()
    {
    }
    
    public QualificationWork(string name, int studentId, int teacherId)
    {
        Name = name;
        StudentId = studentId;
        TeacherId = teacherId;
    }
}
