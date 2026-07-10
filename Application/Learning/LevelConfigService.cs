using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class LevelConfigService
{
    private readonly YourRhythmDbContext _db;

    public LevelConfigService(YourRhythmDbContext db) => _db = db;

    public async Task<IReadOnlyList<LevelConfigSummary>> GetAllForTeacherAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var configs = await _db.LevelConfigs
            .AsNoTracking()
            .Where(lc => lc.SchoolId == schoolId && lc.TeacherProfileId == teacherProfileId)
            .ToDictionaryAsync(lc => lc.Level, cancellationToken);

        return LearningLevelCalculator.Levels
            .Select(def => ToSummary(def, configs.GetValueOrDefault(def.Level)))
            .ToArray();
    }

    public async Task<LevelConfigSummary> GetForLevelAsync(
        AuthenticatedUserProfile profile,
        int level,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var def = LearningLevelCalculator.GetLevel(level);
        var config = await _db.LevelConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(lc => lc.SchoolId == schoolId
                && lc.TeacherProfileId == teacherProfileId
                && lc.Level == level, cancellationToken);

        return ToSummary(def, config);
    }

    public async Task<LevelConfigSummary> UpsertAsync(
        AuthenticatedUserProfile profile,
        UpdateLevelConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Level is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(request.Level), "Level must be between 1 and 5.");

        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var now = DateTime.UtcNow;

        var config = await _db.LevelConfigs.FirstOrDefaultAsync(
            lc => lc.SchoolId == schoolId
                && lc.TeacherProfileId == teacherProfileId
                && lc.Level == request.Level,
            cancellationToken);

        if (config is null)
        {
            config = new LevelConfig(schoolId, teacherProfileId, request.Level, now);
            _db.LevelConfigs.Add(config);
        }

        config.Update(
            request.Subtitle,
            request.Description,
            request.TeacherExpectations,
            request.Objectives,
            request.ConquestMessage,
            request.OrientationMessage,
            now);

        await _db.SaveChangesAsync(cancellationToken);

        var def = LearningLevelCalculator.GetLevel(request.Level);
        return ToSummary(def, config);
    }

    public async Task<IReadOnlyList<LevelConfigSummary>> GetAllForStudentAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, _) = LearningAuthorization.RequireStudent(profile);

        var configs = await _db.LevelConfigs
            .AsNoTracking()
            .Where(lc => lc.SchoolId == schoolId)
            .ToDictionaryAsync(lc => lc.Level, cancellationToken);

        return LearningLevelCalculator.Levels
            .Select(def => ToSummary(def, configs.GetValueOrDefault(def.Level)))
            .ToArray();
    }

    public async Task<LevelUpEventDto?> GetPendingLevelUpAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var ev = await _db.LevelUpEvents
            .AsNoTracking()
            .Where(e => e.SchoolId == schoolId
                && e.StudentProfileId == studentProfileId
                && e.SeenAtUtc == null)
            .OrderBy(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (ev is null) return null;

        var config = await _db.LevelConfigs
            .AsNoTracking()
            .Where(lc => lc.SchoolId == schoolId && lc.Level == ev.ToLevel)
            .Select(lc => lc.ConquestMessage)
            .FirstOrDefaultAsync(cancellationToken);

        return new LevelUpEventDto(
            ev.Id,
            ev.FromLevel,
            ev.ToLevel,
            LearningLevelCalculator.LevelName(ev.FromLevel),
            LearningLevelCalculator.LevelName(ev.ToLevel),
            config,
            ev.CreatedAtUtc);
    }

    public async Task DismissLevelUpAsync(
        AuthenticatedUserProfile profile,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var ev = await _db.LevelUpEvents.FirstOrDefaultAsync(
            e => e.Id == eventId
                && e.SchoolId == schoolId
                && e.StudentProfileId == studentProfileId
                && e.SeenAtUtc == null,
            cancellationToken);

        if (ev is null) return;

        ev.MarkSeen(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static LevelConfigSummary ToSummary(
        Application.Learning.LevelDefinition def,
        LevelConfig? config) =>
        new(
            def.Level,
            def.Name,
            def.MinXp,
            def.MaxXp,
            config?.Subtitle,
            config?.Description,
            config?.TeacherExpectations,
            config?.Objectives,
            config?.ConquestMessage,
            config?.OrientationMessage);
}
