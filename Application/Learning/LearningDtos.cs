using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Application.Learning;

public sealed record TeacherStudentSummary(
    Guid StudentProfileId,
    Guid SchoolUserId,
    string DisplayName,
    string Email,
    string Instrument,
    string Level,
    string Notes,
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
    DateTime? CompletedAtUtc,
    LessonStatus Status,
    string? Notes);

public sealed record LessonDetailSummary(
    LessonSummary Lesson,
    TeacherStudentSummary Student);

public sealed record RepertoireSummary(
    Guid Id,
    string Title,
    RepertoireStatus Status,
    int ProgressPercent,
    string? Notes,
    string? ReferenceUrl,
    string? AudioOriginalFileName,
    string? AudioContentType,
    long? AudioSizeBytes,
    bool HasAudio,
    DateTime CreatedAtUtc);

public sealed record AssignmentSummary(
    Guid Id,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    AssignmentStatus Status,
    DateTime? CompletedAtUtc,
    int XpReward,
    bool XpGranted,
    AssignmentRarity Rarity,
    Guid? SkillRewardId);

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
    string CurrentLevelName,
    int CurrentLevelMinXp,
    int CurrentLevelMaxXp,
    int XpInCurrentLevel,
    int XpRequiredForCurrentLevel,
    int CurrentLevelProgressPercent,
    bool AwaitingPromotion,
    string? NextLevelName,
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
    string? Contact,
    string Instrument,
    string Level,
    string Notes);

public sealed record UpdateTeacherStudentRequest(
    Guid StudentProfileId,
    string DisplayName,
    string Instrument,
    string Level,
    string Notes);

public sealed record CreateLessonRequest(
    Guid StudentProfileId,
    string Title,
    DateTime LessonDateUtc,
    string? Notes);

public sealed record UpdateLessonRequest(
    Guid StudentProfileId,
    Guid LessonId,
    string Title,
    DateTime LessonDateUtc,
    string? Notes);

public sealed record AddRepertoireRequest(
    Guid StudentProfileId,
    string Title,
    string? Notes,
    string? ReferenceUrl,
    RepertoireAudioUpload? Audio);

public sealed record UpdateRepertoireRequest(
    Guid StudentProfileId,
    Guid RepertoireItemId,
    string Title,
    string? Notes,
    string? ReferenceUrl,
    RepertoireAudioUpload? Audio);

public sealed record RepertoireAudioUpload(
    string FileName,
    string ContentType,
    long Length,
    Func<Stream> OpenReadStream);

public sealed record RepertoireAudioFile(
    string PhysicalPath,
    string ContentType,
    string DownloadFileName);

public sealed record UpdateRepertoireProgressRequest(
    Guid RepertoireItemId,
    int ProgressPercent);

public sealed record CreateAssignmentRequest(
    Guid StudentProfileId,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    int XpReward,
    AssignmentRarity Rarity = AssignmentRarity.Comum,
    Guid? SkillRewardId = null);

public sealed record UpdateAssignmentRequest(
    Guid StudentProfileId,
    Guid AssignmentId,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    int XpReward,
    AssignmentRarity Rarity = AssignmentRarity.Comum,
    Guid? SkillRewardId = null);

public sealed record CreateFeedbackRequest(
    Guid StudentProfileId,
    string Message,
    bool VisibleToStudent);

public sealed record UpdateFeedbackRequest(
    Guid StudentProfileId,
    Guid FeedbackId,
    string Message,
    bool VisibleToStudent);

public sealed record SkillSummary(
    Guid Id,
    string Name,
    string? Description,
    int RequiredLevel,
    SkillType SkillType,
    string? IconName,
    string? AchievementText,
    string? ConquestCriteria,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record SkillWithMastery(
    Guid Id,
    string Name,
    string? Description,
    int RequiredLevel,
    SkillType SkillType,
    string? IconName,
    string? AchievementText,
    string? ConquestCriteria,
    bool Mastered,
    DateTime? MasteredAtUtc,
    bool InferredFromCurrentLevel);

public sealed record LevelConfigSummary(
    int Level,
    string LevelName,
    int MinXp,
    int MaxXp,
    string? Subtitle,
    string? Description,
    string? TeacherExpectations,
    string? Objectives,
    string? ConquestMessage,
    string? OrientationMessage);

public sealed record UpdateLevelConfigRequest(
    int Level,
    string? Subtitle,
    string? Description,
    string? TeacherExpectations,
    string? Objectives,
    string? ConquestMessage,
    string? OrientationMessage);

public sealed record LevelUpEventDto(
    Guid Id,
    int FromLevel,
    int ToLevel,
    string FromLevelName,
    string ToLevelName,
    string? ConquestMessage,
    DateTime CreatedAtUtc);

public sealed record CreateSkillRequest(
    string Name,
    string? Description,
    int RequiredLevel,
    SkillType SkillType,
    string? IconName,
    string? AchievementText,
    string? ConquestCriteria);

public sealed record UpdateSkillRequest(
    Guid SkillId,
    string Name,
    string? Description,
    int RequiredLevel,
    SkillType SkillType,
    string? IconName,
    string? AchievementText,
    string? ConquestCriteria);
