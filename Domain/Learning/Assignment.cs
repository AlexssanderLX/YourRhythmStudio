using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class Assignment
{
    private Assignment()
    {
    }

    public Assignment(
        Guid schoolId,
        Guid teacherProfileId,
        Guid studentProfileId,
        string title,
        string description,
        DateTime? dueAtUtc,
        int xpReward,
        DateTime utcNow,
        Guid? lessonId = null,
        Guid? repertoireItemId = null,
        AssignmentRarity rarity = AssignmentRarity.Comum,
        Guid? skillRewardId = null,
        bool isMission = false)
    {
        if (schoolId == Guid.Empty)
            throw new ArgumentException("SchoolId is required.", nameof(schoolId));

        if (teacherProfileId == Guid.Empty)
            throw new ArgumentException("TeacherProfileId is required.", nameof(teacherProfileId));

        if (studentProfileId == Guid.Empty)
            throw new ArgumentException("StudentProfileId is required.", nameof(studentProfileId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Assignment title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Assignment description is required.", nameof(description));

        if (xpReward < 0)
            throw new ArgumentOutOfRangeException(nameof(xpReward), "XP reward cannot be negative.");

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        StudentProfileId = studentProfileId;
        LessonId = lessonId;
        RepertoireItemId = repertoireItemId;
        Title = title.Trim();
        Description = description.Trim();
        DueAtUtc = dueAtUtc;
        XpReward = xpReward;
        Rarity = rarity;
        SkillRewardId = skillRewardId;
        Status = AssignmentStatus.Pending;
        IsMission = isMission;
        CurrentRound = 1;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid TeacherProfileId { get; private set; }

    public Guid StudentProfileId { get; private set; }

    public Guid? LessonId { get; private set; }

    public Guid? RepertoireItemId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTime? DueAtUtc { get; private set; }

    public AssignmentStatus Status { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public int XpReward { get; private set; }

    public bool XpGranted { get; private set; }

    public AssignmentRarity Rarity { get; private set; }

    public Guid? SkillRewardId { get; private set; }

    public bool IsMission { get; private set; }

    public int CurrentRound { get; private set; }

    public DateTime? SubmittedForReviewAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
        string title,
        string description,
        DateTime? dueAtUtc,
        int xpReward,
        DateTime utcNow)
    {
        if (Status == AssignmentStatus.Completed)
            throw new InvalidOperationException("Completed assignments cannot be edited.");

        if (Status == AssignmentStatus.Skipped)
            throw new InvalidOperationException("Skipped assignments cannot be edited.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Assignment title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Assignment description is required.", nameof(description));

        if (xpReward < 0)
            throw new ArgumentOutOfRangeException(nameof(xpReward), "XP reward cannot be negative.");

        Title = title.Trim();
        Description = description.Trim();
        DueAtUtc = dueAtUtc;
        XpReward = xpReward;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateRewardMetadata(AssignmentRarity rarity, Guid? skillRewardId, DateTime utcNow)
    {
        if (Status == AssignmentStatus.Completed)
            throw new InvalidOperationException("Completed assignments cannot be edited.");

        if (Status == AssignmentStatus.Skipped)
            throw new InvalidOperationException("Skipped assignments cannot be edited.");

        Rarity = rarity;
        SkillRewardId = skillRewardId;
        UpdatedAtUtc = utcNow;
    }

    public void Start(DateTime utcNow)
    {
        if (Status == AssignmentStatus.Completed)
            return;

        if (Status == AssignmentStatus.Skipped)
            throw new InvalidOperationException("Skipped assignments cannot be started.");

        Status = AssignmentStatus.InProgress;
        UpdatedAtUtc = utcNow;
    }

    public void Complete(DateTime utcNow)
    {
        if (Status == AssignmentStatus.Completed)
            return;

        if (Status == AssignmentStatus.Skipped)
            throw new InvalidOperationException("Skipped assignments cannot be completed.");

        Status = AssignmentStatus.Completed;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkXpGranted()
    {
        if (XpGranted)
            return;

        XpGranted = true;
    }

    public void Skip(DateTime utcNow)
    {
        if (Status == AssignmentStatus.Completed)
            throw new InvalidOperationException("Completed assignments cannot be skipped.");

        if (Status == AssignmentStatus.Skipped)
            return;

        Status = AssignmentStatus.Skipped;
        UpdatedAtUtc = utcNow;
    }

    public void SubmitForReview(DateTime utcNow)
    {
        if (!IsMission)
            throw new InvalidOperationException("Apenas missoes podem ser enviadas para revisao.");

        if (Status == AssignmentStatus.Completed)
            throw new InvalidOperationException("Missao ja concluida.");

        if (Status == AssignmentStatus.AwaitingReview)
            return;

        Status = AssignmentStatus.AwaitingReview;
        SubmittedForReviewAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void RequestAdjustments(DateTime utcNow)
    {
        if (!IsMission)
            throw new InvalidOperationException("Apenas missoes passam por revisao.");

        if (Status != AssignmentStatus.AwaitingReview)
            throw new InvalidOperationException("Missao nao esta aguardando revisao.");

        Status = AssignmentStatus.AdjustmentsRequested;
        CurrentRound += 1;
        UpdatedAtUtc = utcNow;
    }

    public void Approve(DateTime utcNow)
    {
        if (!IsMission)
            throw new InvalidOperationException("Apenas missoes passam por aprovacao.");

        if (Status == AssignmentStatus.Completed)
            return;

        if (Status != AssignmentStatus.AwaitingReview)
            throw new InvalidOperationException("Missao nao esta aguardando revisao.");

        Status = AssignmentStatus.Completed;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
