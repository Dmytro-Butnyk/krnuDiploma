using Core.Domain.Entities.ArchiveGroup;

namespace DiplomaControlSystem.Api.Contracts.Common;

public sealed record DefenceQuestionDto(string AskedBy, string Text)
{
    public static DefenceQuestionDto From(DefenceQuestion question)
    {
        return new DefenceQuestionDto(question.AskedBy, question.Text);
    }

    public DefenceQuestion ToDomain()
    {
        return new DefenceQuestion(AskedBy.Trim(), Text.Trim());
    }
}
