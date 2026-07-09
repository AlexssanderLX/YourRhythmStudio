using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class SkillService
{
    private readonly YourRhythmDbContext _db;

    public SkillService(YourRhythmDbContext db) => _db = db;

    public async Task<SkillSummary> CreateSkillAsync(
        AuthenticatedUserProfile profile,
        string name,
        string? description,
        int requiredLevel,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var skill = new Skill(schoolId, teacherProfileId, name, description, requiredLevel, DateTime.UtcNow);
        _db.Skills.Add(skill);
        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(skill);
    }

    public async Task DeleteSkillAsync(
        AuthenticatedUserProfile profile,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var skill = await _db.Skills.FirstOrDefaultAsync(
            s => s.Id == skillId && s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId,
            cancellationToken);
        if (skill is null) return;
        _db.Skills.Remove(skill);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SkillSummary>> ListSkillsAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        return await _db.Skills
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId)
            .OrderBy(s => s.RequiredLevel).ThenBy(s => s.Name)
            .Select(s => new SkillSummary(s.Id, s.Name, s.Description, s.RequiredLevel, s.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SkillWithMastery>> GetStudentSkillsAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _db, schoolId, teacherProfileId, studentProfileId, cancellationToken);

        var currentLevel = await _db.StudentProfiles
            .AsNoTracking()
            .Where(student => student.SchoolId == schoolId && student.Id == studentProfileId)
            .Select(student => student.CurrentLevel)
            .FirstOrDefaultAsync(cancellationToken);

        var skills = await _db.Skills
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId)
            .OrderBy(s => s.RequiredLevel).ThenBy(s => s.Name)
            .ToArrayAsync(cancellationToken);

        var masteries = await _db.StudentSkillMasteries
            .AsNoTracking()
            .Where(m => m.SchoolId == schoolId && m.StudentProfileId == studentProfileId)
            .ToArrayAsync(cancellationToken);

        var masteryMap = masteries.ToDictionary(m => m.SkillId, m => m.MasteredAtUtc);

        return skills.Select(s => new SkillWithMastery(
            s.Id, s.Name, s.Description, s.RequiredLevel,
            s.RequiredLevel <= currentLevel || masteryMap.ContainsKey(s.Id),
            masteryMap.TryGetValue(s.Id, out var d) ? d : null,
            s.RequiredLevel <= currentLevel))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<SkillWithMastery>> GetStudentSkillsForStudentAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var currentLevel = await _db.StudentProfiles
            .AsNoTracking()
            .Where(student => student.SchoolId == schoolId && student.Id == studentProfileId)
            .Select(student => student.CurrentLevel)
            .FirstOrDefaultAsync(cancellationToken);

        var teacherLink = await _db.TeacherStudents
            .AsNoTracking()
            .FirstOrDefaultAsync(ts => ts.SchoolId == schoolId
                && ts.StudentProfileId == studentProfileId
                && ts.IsActive,
                cancellationToken);
        if (teacherLink is null) return Array.Empty<SkillWithMastery>();

        var skills = await _db.Skills
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && s.TeacherProfileId == teacherLink.TeacherProfileId)
            .OrderBy(s => s.RequiredLevel).ThenBy(s => s.Name)
            .ToArrayAsync(cancellationToken);

        var masteries = await _db.StudentSkillMasteries
            .AsNoTracking()
            .Where(m => m.SchoolId == schoolId && m.StudentProfileId == studentProfileId)
            .ToArrayAsync(cancellationToken);

        var masteryMap = masteries.ToDictionary(m => m.SkillId, m => m.MasteredAtUtc);

        return skills.Select(s => new SkillWithMastery(
            s.Id, s.Name, s.Description, s.RequiredLevel,
            s.RequiredLevel <= currentLevel || masteryMap.ContainsKey(s.Id),
            masteryMap.TryGetValue(s.Id, out var d) ? d : null,
            s.RequiredLevel <= currentLevel))
            .ToArray();
    }

    public async Task ToggleMasteryAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _db, schoolId, teacherProfileId, studentProfileId, cancellationToken);

        var skillExists = await _db.Skills.AnyAsync(
            s => s.Id == skillId && s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId,
            cancellationToken);
        if (!skillExists)
        {
            throw new KeyNotFoundException("Skill was not found.");
        }

        var existing = await _db.StudentSkillMasteries.FirstOrDefaultAsync(
            m => m.SchoolId == schoolId && m.StudentProfileId == studentProfileId && m.SkillId == skillId,
            cancellationToken);

        if (existing is not null)
        {
            _db.StudentSkillMasteries.Remove(existing);
        }
        else
        {
            _db.StudentSkillMasteries.Add(
                new StudentSkillMastery(schoolId, teacherProfileId, studentProfileId, skillId, DateTime.UtcNow));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SkillSummary ToSummary(Skill s) =>
        new(s.Id, s.Name, s.Description, s.RequiredLevel, s.CreatedAtUtc);
}
