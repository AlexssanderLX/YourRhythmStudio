using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Application.Learning;

public sealed record TeacherStudentSummary(
    Guid StudentProfileId,
    Guid SchoolUserId,
    string DisplayName,
    string Email,
    string Instrument,
    string Level,
    int CurrentXp,
    int CurrentLevel,
    int RepertoireProgressPercent,
    string? CurrentRepertoireTitle);

public sealed record StudentDetailSummary(
    TeacherStudentSummary Student,
    IReadOnlyCollection<LessonSummary> Lessons,
    IReadOnlyCollection<RepertoireSummary> Repertoire,
    IReadOnlyCollection<AssignmentSummary> Assignments,
    IReadOnlyCollection<FeedbackSummary> Feedback);

public sealed record LessonSummary(
    Guid Id,
    Guid StudentProfileId,
    string Title,
    DateTime LessonDateUtc,
    int DurationMinutes,
    DateTime? CompletedAtUtc,
    LessonStatus Status,
    string? Notes);

public sealed record LessonDetailSummary(
    LessonSummary Lesson,
    TeacherStudentSummary Student);

public sealed record RepertoireSummary(
    Guid Id,
    string Title,
    string? ComposerOrArtist,
    string? Instrument,
    string? Level,
    RepertoireStatus Status,
    int ProgressPercent,
    string? Notes,
    string? ReferenceUrl);

public sealed record AssignmentSummary(
    Guid Id,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    AssignmentStatus Status,
    int TargetMinutes,
    DateTime? CompletedAtUtc,
    int XpReward,
    bool XpGranted);

public sealed record FeedbackSummary(
    Guid Id,
    string Message,
    bool VisibleToStudent,
    DateTime CreatedAtUtc);

public sealed record XpEventSummary(
    Guid Id,
    int Points,
    string Description,
    DateTime CreatedAtUtc);

public sealed record ProgressSummary(
    int CurrentXp,
    int CurrentLevel,
    int PendingAssignments,
    int CompletedAssignments,
    int RepertoireInProgress,
    IReadOnlyCollection<XpEventSummary> RecentXpEvents);

public sealed record TeacherDashboardSummary(
    int ActiveStudentCount,
    int PendingAssignmentCount,
    int RecentCompletedAssignmentCount,
    IReadOnlyCollection<TeacherStudentSummary> RecentStudents,
    IReadOnlyCollection<LessonSummary> RecentLessons);

public sealed record StudentDashboardSummary(
    ProgressSummary Progress,
    IReadOnlyCollection<AssignmentSummary> PendingAssignments,
    IReadOnlyCollection<AssignmentSummary> CompletedAssignments,
    IReadOnlyCollection<RepertoireSummary> RepertoireInProgress,
    IReadOnlyCollection<FeedbackSummary> RecentFeedback);

public sealed record CreateTeacherStudentRequest(
    string DisplayName,
    string Email,
    string Instrument,
    string Level,
    string Notes);

public sealed record CreateLessonRequest(
    Guid StudentProfileId,
    string Title,
    DateTime LessonDateUtc,
    int DurationMinutes,
    string? Notes);

public sealed record AddRepertoireRequest(
    Guid StudentProfileId,
    string Title,
    string? ComposerOrArtist,
    string? Instrument,
    string? Level,
    string? Notes,
    string? ReferenceUrl);

public sealed record UpdateRepertoireProgressRequest(
    Guid RepertoireItemId,
    int ProgressPercent);

public sealed record CreateAssignmentRequest(
    Guid StudentProfileId,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    int TargetMinutes,
    int XpReward);

public sealed record CreateFeedbackRequest(
    Guid StudentProfileId,
    string Message,
    bool VisibleToStudent);

public sealed record SkillSummary(
    Guid Id,
    string Name,
    string? Description,
    int RequiredLevel,
    DateTime CreatedAtUtc);

public sealed record SkillWithMastery(
    Guid Id,
    string Name,
    string? Description,
    int RequiredLevel,
    bool Mastered,
    DateTime? MasteredAtUtc,
    bool InferredFromCurrentLevel);
