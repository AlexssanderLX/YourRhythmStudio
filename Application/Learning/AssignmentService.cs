using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
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

        var now = DateTime.UtcNow;
        var assignment = new Assignment(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Title,
            request.Description,
            request.DueAtUtc,
            request.XpReward,
            now);

        assignment.UpdateDetails(
            request.Title,
            request.Description,
            request.DueAtUtc,
            request.TargetMinutes,
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

        var student = await _dbContext.StudentProfiles.FirstAsync(
            item => item.Id == studentProfileId && item.SchoolId == schoolId,
            cancellationToken);

        assignment.Complete(DateTime.UtcNow);

        if (!assignment.XpGranted && assignment.XpReward > 0)
        {
            student.CurrentXp += assignment.XpReward;
            student.CurrentLevel = LearningLevelCalculator.CalculateLevel(student.CurrentXp);
            assignment.MarkXpGranted();

            _dbContext.XpEvents.Add(new XpEvent(
                schoolId,
                studentProfileId,
                XpEventType.AssignmentCompleted,
                assignment.XpReward,
                $"Missao concluida: {assignment.Title}",
                DateTime.UtcNow,
                assignment.TeacherProfileId,
                assignment.Id));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AssignmentSummary> QueryAssignments(Guid schoolId, Guid studentProfileId, Guid? teacherProfileId)
    {
        var query = _dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.SchoolId == schoolId && assignment.StudentProfileId == studentProfileId);

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
                assignment.TargetMinutes,
                assignment.CompletedAtUtc,
                assignment.XpReward,
                assignment.XpGranted));
    }

    private static AssignmentSummary ToSummary(Assignment assignment)
    {
        return new AssignmentSummary(
            assignment.Id,
            assignment.Title,
            assignment.Description,
            assignment.DueAtUtc,
            assignment.Status,
            assignment.TargetMinutes,
            assignment.CompletedAtUtc,
            assignment.XpReward,
            assignment.XpGranted);
    }
}
