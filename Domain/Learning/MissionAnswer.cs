namespace YourRhythmStudio.Domain.Learning;

public sealed class MissionAnswer
{
    private MissionAnswer()
    {
    }

    public MissionAnswer(Guid assignmentId, Guid questionId, Guid studentProfileId, int roundNumber)
    {
        Id = Guid.NewGuid();
        AssignmentId = assignmentId;
        QuestionId = questionId;
        StudentProfileId = studentProfileId;
        RoundNumber = roundNumber;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public int RoundNumber { get; private set; }
    public string? AnswerText { get; set; }
    public string? StoredFileName { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
