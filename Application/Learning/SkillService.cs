using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class SkillService
{
    private readonly YourRhythmDbContext _db;

    public SkillService(YourRhythmDbContext db) => _db = db;

    public async Task<SkillSummary> CreateSkillAsync(
        AuthenticatedUserProfile profile,
        CreateSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var skill = new Skill(
            schoolId,
            teacherProfileId,
            request.Name,
            request.Description,
            request.RequiredLevel,
            request.SkillType,
            request.IconName,
            request.AchievementText,
            request.ConquestCriteria,
            sortOrder: 0,
            DateTime.UtcNow);

        _db.Skills.Add(skill);
        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(skill);
    }

    public async Task<bool> DeleteSkillAsync(
        AuthenticatedUserProfile profile,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var skill = await _db.Skills.FirstOrDefaultAsync(
            s => s.Id == skillId && s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId,
            cancellationToken);
        if (skill is null) return false;

        var hasLinkedData = await _db.StudentSkillMasteries
            .AnyAsync(m => m.SkillId == skillId, cancellationToken)
            || await _db.Assignments
            .AnyAsync(a => a.SkillRewardId == skillId, cancellationToken);

        if (hasLinkedData)
        {
            // Soft-delete: deactivate instead of destroy
            skill.Deactivate(DateTime.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return false; // indicates deactivated, not deleted
        }

        _db.Skills.Remove(skill);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<SkillSummary>> ListSkillsAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        return await _db.Skills
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId)
            .OrderBy(s => s.RequiredLevel).ThenBy(s => s.SortOrder).ThenBy(s => s.SkillType).ThenBy(s => s.Name)
            .Select(s => new SkillSummary(s.Id, s.Name, s.Description, s.RequiredLevel, s.SkillType, s.IconName,
                s.AchievementText, s.ConquestCriteria, s.SortOrder, s.IsActive, s.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SkillSummary> UpdateSkillAsync(
        AuthenticatedUserProfile profile,
        UpdateSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var skill = await _db.Skills.FirstOrDefaultAsync(
            s => s.Id == request.SkillId
                && s.SchoolId == schoolId
                && s.TeacherProfileId == teacherProfileId
                && s.IsActive,
            cancellationToken)
            ?? throw new KeyNotFoundException("Skill was not found.");

        skill.Update(
            request.Name,
            request.Description,
            request.RequiredLevel,
            request.SkillType,
            request.IconName,
            request.AchievementText,
            request.ConquestCriteria,
            DateTime.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(skill);
    }

    public async Task<IReadOnlyCollection<SkillWithMastery>> GetStudentSkillsAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _db, schoolId, teacherProfileId, studentProfileId, cancellationToken);

        return await BuildSkillsWithMastery(schoolId, teacherProfileId, studentProfileId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SkillWithMastery>> GetStudentSkillsForStudentAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var teacherLink = await _db.TeacherStudents
            .AsNoTracking()
            .FirstOrDefaultAsync(ts => ts.SchoolId == schoolId
                && ts.StudentProfileId == studentProfileId
                && ts.IsActive,
                cancellationToken);
        if (teacherLink is null) return Array.Empty<SkillWithMastery>();

        return await BuildSkillsWithMastery(schoolId, teacherLink.TeacherProfileId, studentProfileId, cancellationToken);
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

        var skill = await _db.Skills.FirstOrDefaultAsync(
            s => s.Id == skillId && s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId,
            cancellationToken);
        if (skill is null) throw new KeyNotFoundException("Skill was not found.");

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

            if (skill.SkillType == SkillType.PromotionRequired)
            {
                var student = await _db.StudentProfiles.FirstOrDefaultAsync(
                    sp => sp.Id == studentProfileId && sp.SchoolId == schoolId,
                    cancellationToken);

                if (student is not null
                    && skill.RequiredLevel == student.CurrentLevel
                    && LearningLevelCalculator.IsEligibleForPromotion(student.CurrentXp, student.CurrentLevel))
                {
                    var fromLevel = student.CurrentLevel;
                    student.CurrentLevel += 1;

                    _db.LevelUpEvents.Add(new LevelUpEvent(
                        schoolId,
                        studentProfileId,
                        fromLevel,
                        student.CurrentLevel,
                        DateTime.UtcNow));
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<SkillWithMastery>> BuildSkillsWithMastery(
        Guid schoolId,
        Guid teacherProfileId,
        Guid studentProfileId,
        CancellationToken cancellationToken)
    {
        var currentLevel = await _db.StudentProfiles
            .AsNoTracking()
            .Where(sp => sp.SchoolId == schoolId && sp.Id == studentProfileId)
            .Select(sp => sp.CurrentLevel)
            .FirstOrDefaultAsync(cancellationToken);

        var skills = await _db.Skills
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && s.TeacherProfileId == teacherProfileId && s.IsActive)
            .OrderBy(s => s.RequiredLevel).ThenBy(s => s.SortOrder).ThenBy(s => s.SkillType).ThenBy(s => s.Name)
            .ToArrayAsync(cancellationToken);

        var masteries = await _db.StudentSkillMasteries
            .AsNoTracking()
            .Where(m => m.SchoolId == schoolId && m.StudentProfileId == studentProfileId)
            .ToArrayAsync(cancellationToken);

        var masteryMap = masteries.ToDictionary(m => m.SkillId, m => m.MasteredAtUtc);

        return skills.Select(s =>
        {
            var explicitMastery = masteryMap.ContainsKey(s.Id);
            var inferredByLevel = s.SkillType != SkillType.PromotionRequired && s.RequiredLevel <= currentLevel;
            var mastered = explicitMastery || inferredByLevel;

            return new SkillWithMastery(
                s.Id, s.Name, s.Description, s.RequiredLevel, s.SkillType, s.IconName, s.AchievementText,
                s.ConquestCriteria,
                mastered,
                masteryMap.TryGetValue(s.Id, out var d) ? d : null,
                inferredByLevel);
        }).ToArray();
    }

    private static SkillSummary ToSummary(Skill s) =>
        new(s.Id, s.Name, s.Description, s.RequiredLevel, s.SkillType, s.IconName,
            s.AchievementText, s.ConquestCriteria, s.SortOrder, s.IsActive, s.CreatedAtUtc);
}
