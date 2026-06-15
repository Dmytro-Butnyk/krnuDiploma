namespace Core.Domain.Entities.ArchiveGroup;

public sealed class DefenceQuestion
{
    public string AskedBy { get; set; }
    public string Text { get; set; }

    private DefenceQuestion()
    {
        AskedBy = string.Empty;
        Text = string.Empty;
    }

    public DefenceQuestion(string askedBy, string text)
    {
        AskedBy = askedBy;
        Text = text;
    }
}
