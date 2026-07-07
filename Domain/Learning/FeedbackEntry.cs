namespace YourRhythmStudio.Domain.Learning;

public sealed class FeedbackEntry
{
    private FeedbackEntry()
    {
    }

    public FeedbackEntry(
        Guid schoolId,
        Guid teacherProfileId,
        Guid studentProfileId,
        string message,
        bool visibleToStudent,
        DateTime utcNow,
        Guid? lessonId = null,
        Guid? assignmentId = null,
        Guid? repertoireItemId = null)
    {
        if (schoolId == Guid.Empty)
            throw new ArgumentException("SchoolId is required.", nameof(schoolId));

        if (teacherProfileId == Guid.Empty)
            throw new ArgumentException("TeacherProfileId is required.", nameof(teacherProfileId));

        if (studentProfileId == Guid.Empty)
            throw new ArgumentException("StudentProfileId is required.", nameof(studentProfileId));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Feedback message is required.", nameof(message));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        StudentProfileId = studentProfileId;
        LessonId = lessonId;
        AssignmentId = assignmentId;
        RepertoireItemId = repertoireItemId;
        Message = message.Trim();
        VisibleToStudent = visibleToStudent;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid TeacherProfileId { get; private set; }

    public Guid StudentProfileId { get; private set; }

    public Guid? LessonId { get; private set; }

    public Guid? AssignmentId { get; private set; }

    public Guid? RepertoireItemId { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public bool VisibleToStudent { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void UpdateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Feedback message is required.", nameof(message));

        Message = message.Trim();
    }

    public void HideFromStudent()
    {
        if (!VisibleToStudent)
            return;

        VisibleToStudent = false;
    }

    public void ShowToStudent()
    {
        if (VisibleToStudent)
            return;

        VisibleToStudent = true;
    }
}