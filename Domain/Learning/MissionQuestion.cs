using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class MissionQuestion
{
    private MissionQuestion()
    {
    }

    public MissionQuestion(
        Guid assignmentId,
        string questionText,
        MissionQuestionType questionType,
        int order,
        bool isRequired,
        string? optionsJson = null)
    {
        if (assignmentId == Guid.Empty)
            throw new ArgumentException("AssignmentId is required.", nameof(assignmentId));

        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text is required.", nameof(questionText));

        Id = Guid.NewGuid();
        AssignmentId = assignmentId;
        QuestionText = questionText.Trim();
        QuestionType = questionType;
        Order = order;
        IsRequired = isRequired;
        OptionsJson = string.IsNullOrWhiteSpace(optionsJson) ? null : optionsJson;
    }

    public Guid Id { get; private set; }
    public Guid AssignmentId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public MissionQuestionType QuestionType { get; private set; }
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }
    public string? OptionsJson { get; private set; }
}
