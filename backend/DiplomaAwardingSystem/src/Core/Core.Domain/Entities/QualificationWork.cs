namespace Core.Domain.Entities;

public sealed class QualificationWork : BaseEntity
{
    public required string Name { get; set; }
    public required int StudentId { get; set; }
    public required Student Student { get; set; }
    public required int TeacherId { get; set; }
    public required Teacher Teacher { get; set; }

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
