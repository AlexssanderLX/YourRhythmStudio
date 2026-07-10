using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class Lesson
{
    private Lesson()
    {
    }

    public Lesson(
        Guid schoolId,
        Guid teacherProfileId,
        Guid studentProfileId,
        string title,
        DateTime lessonDateUtc,
        DateTime utcNow)
    {
        if (schoolId == Guid.Empty)
            throw new ArgumentException("SchoolId is required.", nameof(schoolId));

        if (teacherProfileId == Guid.Empty)
            throw new ArgumentException("TeacherProfileId is required.", nameof(teacherProfileId));

        if (studentProfileId == Guid.Empty)
            throw new ArgumentException("StudentProfileId is required.", nameof(studentProfileId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Lesson title is required.", nameof(title));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        StudentProfileId = studentProfileId;
        Title = title.Trim();
        LessonDateUtc = lessonDateUtc;
        Status = LessonStatus.Planned;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid TeacherProfileId { get; private set; }

    public Guid StudentProfileId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateTime LessonDateUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public LessonStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
        string title,
        DateTime lessonDateUtc,
        string? notes,
        DateTime utcNow)
    {
        if (Status == LessonStatus.Completed)
            throw new InvalidOperationException("Completed lessons cannot be edited.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Lesson title is required.", nameof(title));

        Title = title.Trim();
        LessonDateUtc = lessonDateUtc;
        Notes = NormalizeOptionalText(notes);
        UpdatedAtUtc = utcNow;
    }

    public void Complete(string? notes, DateTime utcNow)
    {
        if (Status == LessonStatus.Completed)
            return;

        if (Status == LessonStatus.Cancelled)
            throw new InvalidOperationException("Cancelled lessons cannot be completed.");

        Status = LessonStatus.Completed;
        CompletedAtUtc = utcNow;

        var normalizedNotes = NormalizeOptionalText(notes);
        if (normalizedNotes is not null)
            Notes = normalizedNotes;

        UpdatedAtUtc = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status == LessonStatus.Completed)
            throw new InvalidOperationException("Completed lessons cannot be cancelled.");

        if (Status == LessonStatus.Cancelled)
            return;

        Status = LessonStatus.Cancelled;
        UpdatedAtUtc = utcNow;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
