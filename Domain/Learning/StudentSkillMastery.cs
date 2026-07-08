namespace YourRhythmStudio.Domain.Learning;

public sealed class StudentSkillMastery
{
    private StudentSkillMastery() { }

    public StudentSkillMastery(Guid schoolId, Guid teacherProfileId, Guid studentProfileId, Guid skillId, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        StudentProfileId = studentProfileId;
        SkillId = skillId;
        MasteredAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid TeacherProfileId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public Guid SkillId { get; private set; }
    public DateTime MasteredAtUtc { get; private set; }
}
