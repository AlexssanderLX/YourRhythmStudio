namespace YourRhythmStudio.Domain;

public static class YourRhythmFeatures
{
    public const string Students = "students";
    public const string Teachers = "teachers";
    public const string Repertoire = "repertoire";
    public const string Materials = "materials";
    public const string Lessons = "lessons";
    public const string Gamification = "gamification";
    public const string SchoolDashboard = "school-dashboard";
    public const string Scheduling = "scheduling";
    public const string Communication = "communication";
    public const string LearningPaths = "learning-paths";

    public static readonly IReadOnlyCollection<string> Mvp =
    [
        Students,
        Teachers,
        Repertoire,
        Materials,
        Lessons,
        Gamification
    ];

    public static readonly IReadOnlyCollection<string> Planned =
    [
        SchoolDashboard,
        Scheduling,
        Communication,
        LearningPaths
    ];
}
