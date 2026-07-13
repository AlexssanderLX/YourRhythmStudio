using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;


namespace YourRhythmStudio.Application.Learning;

public sealed class AssignmentService
{
    private readonly YourRhythmDbContext _dbContext;

    public AssignmentService(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AssignmentSummary> CreateAssignmentAsync(
        AuthenticatedUserProfile profile,
        CreateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            cancellationToken);

        // Only Epic and Legendary assignments may carry a skill reward.
        if (request.SkillRewardId.HasValue
            && request.Rarity is not (AssignmentRarity.Epica or AssignmentRarity.Lendaria))
        {
            throw new InvalidOperationException(
                "Recompensa de habilidade só pode ser vinculada a missões Épicas ou Lendárias.");
        }

        // Validate that the skill belongs to this teacher's school.
        if (request.SkillRewardId.HasValue)
        {
            var skillExists = await _dbContext.Skills.AnyAsync(
                s => s.Id == request.SkillRewardId.Value
                    && s.SchoolId == schoolId
                    && s.TeacherProfileId == teacherProfileId
                    && s.IsActive,
                cancellationToken);
            if (!skillExists)
                throw new KeyNotFoundException("Habilidade selecionada não encontrada.");
        }

        var now = DateTime.UtcNow;
        var assignment = new Assignment(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Title,
            request.Description,
            request.DueAtUtc,
            request.XpReward,
            now,
            rarity: request.Rarity,
            skillRewardId: request.SkillRewardId);

        assignment.UpdateDetails(
            request.Title,
            request.Description,
            request.DueAtUtc,
            request.XpReward,
            now);

        _dbContext.Assignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToSummary(assignment);
    }

    public async Task<IReadOnlyCollection<AssignmentSummary>> ListForTeacherStudentAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            studentProfileId,
            cancellationToken);

        return await QueryAssignments(schoolId, studentProfileId, teacherProfileId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssignmentSummary>> ListForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        return await QueryAssignments(schoolId, studentProfileId, null).ToArrayAsync(cancellationToken);
    }

    public async Task<AssignmentSummary> UpdateAssignmentAsync(
        AuthenticatedUserProfile profile,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            cancellationToken);

        if (request.SkillRewardId.HasValue
            && request.Rarity is not (AssignmentRarity.Epica or AssignmentRarity.Lendaria))
        {
            throw new InvalidOperationException(
                "Recompensa de habilidade so pode ser vinculada a missoes Epicas ou Lendarias.");
        }

        if (request.SkillRewardId.HasValue)
        {
            var skillExists = await _dbContext.Skills.AnyAsync(
                skill => skill.Id == request.SkillRewardId.Value
                    && skill.SchoolId == schoolId
                    && skill.TeacherProfileId == teacherProfileId
                    && skill.IsActive,
                cancellationToken);
            if (!skillExists)
                throw new KeyNotFoundException("Habilidade selecionada nao encontrada.");
        }

        var assignment = await _dbContext.Assignments.FirstOrDefaultAsync(
            item => item.Id == request.AssignmentId
                && item.SchoolId == schoolId
                && item.TeacherProfileId == teacherProfileId
                && item.StudentProfileId == request.StudentProfileId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Assignment was not found.");

        var now = DateTime.UtcNow;
        assignment.UpdateDetails(
            request.Title,
            string.IsNullOrWhiteSpace(request.Description) ? request.Title : request.Description,
            request.DueAtUtc,
            request.XpReward,
            now);
        assignment.UpdateRewardMetadata(request.Rarity, request.SkillRewardId, now);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(assignment);
    }

    public async Task StartAssignmentAsync(
        AuthenticatedUserProfile profile,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        var assignment = await _dbContext.Assignments.FirstOrDefaultAsync(
            item => item.Id == assignmentId && item.SchoolId == schoolId && item.StudentProfileId == studentProfileId,
            cancellationToken);

        if (assignment is null)
        {
            throw new KeyNotFoundException("Assignment was not found.");
        }

        assignment.Start(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAssignmentAsync(
        AuthenticatedUserProfile profile,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        var assignment = await _dbContext.Assignments.FirstOrDefaultAsync(
            item => item.Id == assignmentId && item.SchoolId == schoolId && item.StudentProfileId == studentProfileId,
            cancellationToken);

        if (assignment is null)
        {
            throw new KeyNotFoundException("Assignment was not found.");
        }

        if (assignment.IsMission)
        {
            throw new InvalidOperationException(
                "Missoes devem ser enviadas para revisao do professor. Use a pagina da missao.");
        }

        var student = await _dbContext.StudentProfiles.FirstAsync(
            item => item.Id == studentProfileId && item.SchoolId == schoolId,
            cancellationToken);

        var now = DateTime.UtcNow;
        assignment.Complete(now);

        if (!assignment.XpGranted && assignment.XpReward > 0)
        {
            student.CurrentXp      += assignment.XpReward; // total XP — never resets
            student.CurrentLevelXp += assignment.XpReward; // per-level XP — resets on promotion
            assignment.MarkXpGranted();

            _dbContext.XpEvents.Add(new XpEvent(
                schoolId,
                studentProfileId,
                XpEventType.AssignmentCompleted,
                assignment.XpReward,
                $"Missão concluída: {assignment.Title}",
                now,
                assignment.TeacherProfileId,
                assignment.Id));

            // Auto level-up when no PromotionRequired skill exists for the current level.
            await TryAutoLevelUpAsync(schoolId, studentProfileId, student, now, cancellationToken);
        }

        // Auto-grant the linked skill reward (if any) when not already mastered.
        if (assignment.SkillRewardId.HasValue)
        {
            var alreadyMastered = await _dbContext.StudentSkillMasteries.AnyAsync(
                m => m.SchoolId == schoolId
                    && m.StudentProfileId == studentProfileId
                    && m.SkillId == assignment.SkillRewardId.Value,
                cancellationToken);

            if (!alreadyMastered)
            {
                _dbContext.StudentSkillMasteries.Add(new StudentSkillMastery(
                    schoolId,
                    assignment.TeacherProfileId,
                    studentProfileId,
                    assignment.SkillRewardId.Value,
                    now));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task TryAutoLevelUpAsync(
        Guid schoolId,
        Guid studentProfileId,
        StudentProfile student,
        DateTime now,
        CancellationToken cancellationToken)
    {
        while (LearningLevelCalculator.IsEligibleForPromotion(student.CurrentLevelXp, student.CurrentLevel))
        {
            var hasRequiredSkill = await _dbContext.Skills.AnyAsync(
                s => s.SchoolId == schoolId
                    && s.RequiredLevel == student.CurrentLevel
                    && s.IsActive
                    && s.SkillType == SkillType.PromotionRequired,
                cancellationToken);

            if (hasRequiredSkill)
                break;

            var fromLevel = student.CurrentLevel;
            student.CurrentLevel   += 1;
            student.CurrentLevelXp  = 0; // XP resets to 0 on each promotion

            _dbContext.LevelUpEvents.Add(new LevelUpEvent(
                schoolId,
                studentProfileId,
                fromLevel,
                student.CurrentLevel,
                now));
        }
    }

    private IQueryable<AssignmentSummary> QueryAssignments(Guid schoolId, Guid studentProfileId, Guid? teacherProfileId)
    {
        var query = _dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.SchoolId == schoolId
                && assignment.StudentProfileId == studentProfileId
                && assignment.IsMission);

        if (teacherProfileId is not null)
        {
            query = query.Where(assignment => assignment.TeacherProfileId == teacherProfileId.Value);
        }

        return query
            .OrderByDescending(assignment => assignment.CreatedAtUtc)
            .Select(assignment => new AssignmentSummary(
                assignment.Id,
                assignment.Title,
                assignment.Description,
                assignment.DueAtUtc,
                assignment.Status,
                assignment.CompletedAtUtc,
                assignment.XpReward,
                assignment.XpGranted,
                assignment.Rarity,
                assignment.SkillRewardId));
    }

    private static AssignmentSummary ToSummary(Assignment assignment)
    {
        return new AssignmentSummary(
            assignment.Id,
            assignment.Title,
            assignment.Description,
            assignment.DueAtUtc,
            assignment.Status,
            assignment.CompletedAtUtc,
            assignment.XpReward,
            assignment.XpGranted,
            assignment.Rarity,
            assignment.SkillRewardId);
    }
}
