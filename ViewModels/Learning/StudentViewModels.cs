using YourRhythmStudio.Application.Learning;

namespace YourRhythmStudio.ViewModels.Learning;

public sealed class StudentDashboardViewModel
{
    public required StudentDashboardSummary Summary { get; init; }
}

public sealed class StudentAssignmentsViewModel
{
    public required IReadOnlyCollection<AssignmentSummary> Assignments { get; init; }
}

public sealed class StudentRepertoireViewModel
{
    public required IReadOnlyCollection<RepertoireSummary> Repertoire { get; init; }
}

public sealed class StudentFeedbackViewModel
{
    public required IReadOnlyCollection<FeedbackSummary> Feedback { get; init; }
}

public sealed class StudentProgressViewModel
{
    public required ProgressSummary Progress { get; init; }
}

public sealed class StudentRepertoireDetailViewModel
{
    public required RepertoireSummary Item { get; init; }
}

public sealed class StudentLevelsViewModel
{
    public required ProgressSummary Progress { get; init; }
    public required IReadOnlyCollection<SkillWithMastery> Skills { get; init; }
}
