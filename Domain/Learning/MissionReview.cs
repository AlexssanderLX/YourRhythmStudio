using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class MissionReview
{
    private MissionReview()
    {
    }

    public MissionReview(
        Guid assignmentId,
        Guid teacherProfileId,
        MissionReviewDecision decision,
        string? feedback,
        int roundNumber)
    {
        Id = Guid.NewGuid();
        AssignmentId = assignmentId;
        TeacherProfileId = teacherProfileId;
        Decision = decision;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        RoundNumber = roundNumber;
        ReviewedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid TeacherProfileId { get; private set; }
    public MissionReviewDecision Decision { get; private set; }
    public string? Feedback { get; private set; }
    public int RoundNumber { get; private set; }
    public DateTime ReviewedAtUtc { get; private set; }
}
