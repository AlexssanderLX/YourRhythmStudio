using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class TeacherStudentService
{
    private readonly YourRhythmDbContext _dbContext;

    public TeacherStudentService(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TeacherStudentSummary>> ListStudentsAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var rows = await QueryBaseStudents(schoolId, teacherProfileId, cancellationToken);
        var enriched = await EnrichWithRepertoireAsync(rows, schoolId, cancellationToken);
        return enriched.OrderBy(s => s.DisplayName).ToArray();
    }

    public async Task<StudentDetailSummary> GetStudentDetailAsync(
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

        var rows = await QueryBaseStudents(schoolId, teacherProfileId, cancellationToken);
        var all = await EnrichWithRepertoireAsync(rows, schoolId, cancellationToken);
        var student = all.FirstOrDefault(item => item.StudentProfileId == studentProfileId)
            ?? throw new InvalidOperationException("Student not found for this teacher.");

        var lessons = await _dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.SchoolId == schoolId
                && lesson.TeacherProfileId == teacherProfileId
                && lesson.StudentProfileId == studentProfileId)
            .OrderByDescending(lesson => lesson.LessonDateUtc)
            .Take(10)
            .Select(lesson => new LessonSummary(
                lesson.Id,
                lesson.StudentProfileId,
                lesson.Title,
                lesson.LessonDateUtc,
                lesson.CompletedAtUtc,
                lesson.Status,
                lesson.Notes))
            .ToArrayAsync(cancellationToken);

        var repertoire = await _dbContext.RepertoireItems
            .AsNoTracking()
            .Where(item => item.SchoolId == schoolId
                && item.TeacherProfileId == teacherProfileId
                && item.StudentProfileId == studentProfileId)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Take(10)
            .Select(item => new RepertoireSummary(
                item.Id,
                item.Title,
                item.Status,
                item.ProgressPercent,
                item.Notes,
                item.ReferenceUrl,
                item.AudioOriginalFileName,
                item.AudioContentType,
                item.AudioSizeBytes,
                item.AudioStoredFileName != null,
                item.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        var assignments = await _dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.SchoolId == schoolId
                && assignment.TeacherProfileId == teacherProfileId
                && assignment.StudentProfileId == studentProfileId)
            .OrderByDescending(assignment => assignment.CreatedAtUtc)
            .Take(10)
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
                assignment.SkillRewardId))
            .ToArrayAsync(cancellationToken);

        var feedback = await _dbContext.FeedbackEntries
            .AsNoTracking()
            .Where(entry => entry.SchoolId == schoolId
                && entry.TeacherProfileId == teacherProfileId
                && entry.StudentProfileId == studentProfileId)
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(10)
            .Select(entry => new FeedbackSummary(entry.Id, entry.Message, entry.VisibleToStudent, entry.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new StudentDetailSummary(student, lessons, repertoire, assignments, feedback);
    }

    public async Task<TeacherStudentSummary> UpdateStudentAsync(
        AuthenticatedUserProfile profile,
        UpdateTeacherStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            cancellationToken);

        var studentProfile = await _dbContext.StudentProfiles
            .FirstOrDefaultAsync(
                student => student.SchoolId == schoolId && student.Id == request.StudentProfileId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Student profile was not found.");

        var schoolUser = await _dbContext.SchoolUsers
            .FirstOrDefaultAsync(
                user => user.SchoolId == schoolId && user.Id == studentProfile.SchoolUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException("School user was not found.");

        schoolUser.DisplayName = RequireText(request.DisplayName, nameof(request.DisplayName));
        studentProfile.Instrument = OptionalText(request.Instrument) ?? string.Empty;
        studentProfile.Level = OptionalText(request.Level) ?? string.Empty;
        studentProfile.Notes = OptionalText(request.Notes) ?? string.Empty;
        studentProfile.CurrentLevel = ParseInitialLevel(request.Level);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var rows = await QueryBaseStudents(schoolId, teacherProfileId, cancellationToken);
        var all = await EnrichWithRepertoireAsync(rows, schoolId, cancellationToken);
        return all.First(item => item.StudentProfileId == request.StudentProfileId);
    }

    public async Task DeactivateStudentAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var link = await _dbContext.TeacherStudents
            .FirstOrDefaultAsync(
                item => item.SchoolId == schoolId
                    && item.TeacherProfileId == teacherProfileId
                    && item.StudentProfileId == studentProfileId
                    && item.IsActive,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("Teacher cannot access this student.");

        var studentProfile = await _dbContext.StudentProfiles
            .FirstOrDefaultAsync(
                student => student.SchoolId == schoolId && student.Id == studentProfileId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Student profile was not found.");

        link.Deactivate(DateTime.UtcNow);

        var hasOtherActiveLinks = await _dbContext.TeacherStudents.AnyAsync(
            item => item.SchoolId == schoolId
                && item.StudentProfileId == studentProfileId
                && item.Id != link.Id
                && item.IsActive,
            cancellationToken);

        if (!hasOtherActiveLinks)
        {
            var schoolUser = await _dbContext.SchoolUsers
                .FirstOrDefaultAsync(
                    user => user.SchoolId == schoolId && user.Id == studentProfile.SchoolUserId,
                    cancellationToken);

            if (schoolUser is not null)
            {
                schoolUser.IsActive = false;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TeacherStudentSummary> CreateStudentAsync(
        AuthenticatedUserProfile profile,
        CreateTeacherStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var contact = OptionalText(request.Contact);
        if (LooksLikeEmail(contact) && !IsEmail(contact))
            throw new ArgumentException("Informe um e-mail valido ou deixe o contato como texto sem @.", nameof(request.Contact));

        var contactEmail = IsEmail(contact) ? NormalizeEmail(contact!) : null;
        var technicalEmail = contactEmail ?? BuildAdministrativeEmail();
        var instrument = RequireText(request.Instrument, nameof(request.Instrument));

        var existingSchoolUser = contactEmail is null
            ? null
            : await _dbContext.SchoolUsers
                .FirstOrDefaultAsync(
                    user => user.SchoolId == schoolId && user.Email == contactEmail,
                    cancellationToken);

        StudentProfile studentProfile;
        if (existingSchoolUser is null)
        {
            var schoolUser = new SchoolUser
            {
                SchoolId = schoolId,
                DisplayName = RequireText(request.DisplayName, nameof(request.DisplayName)),
                Email = technicalEmail,
                Role = YourRhythmRoles.Student,
                Phone = contactEmail is null ? contact : null
            };

            studentProfile = new StudentProfile
            {
                SchoolId = schoolId,
                SchoolUserId = schoolUser.Id,
                Instrument = instrument,
                Level = OptionalText(request.Level) ?? string.Empty,
                Notes = OptionalText(request.Notes) ?? string.Empty,
                CurrentLevel = ParseInitialLevel(request.Level)
            };

            _dbContext.SchoolUsers.Add(schoolUser);
            _dbContext.StudentProfiles.Add(studentProfile);
        }
        else
        {
            if (existingSchoolUser.Role != YourRhythmRoles.Student)
            {
                throw new InvalidOperationException("Este e-mail ja esta vinculado a um usuario que nao e aluno.");
            }

            existingSchoolUser.DisplayName = RequireText(request.DisplayName, nameof(request.DisplayName));
            existingSchoolUser.IsActive = true;
            if (contactEmail is null)
                existingSchoolUser.Phone = contact;

            var existingStudent = await _dbContext.StudentProfiles
                .FirstOrDefaultAsync(
                    student => student.SchoolId == schoolId && student.SchoolUserId == existingSchoolUser.Id,
                    cancellationToken);

            if (existingStudent is null)
            {
                studentProfile = new StudentProfile
                {
                    SchoolId = schoolId,
                    SchoolUserId = existingSchoolUser.Id,
                    Instrument = instrument,
                    Level = OptionalText(request.Level) ?? string.Empty,
                    Notes = OptionalText(request.Notes) ?? string.Empty,
                    CurrentLevel = ParseInitialLevel(request.Level)
                };

                _dbContext.StudentProfiles.Add(studentProfile);
            }
            else
            {
                studentProfile = existingStudent;
                studentProfile.Instrument = instrument;
                studentProfile.Level = OptionalText(request.Level) ?? studentProfile.Level;
                studentProfile.Notes = OptionalText(request.Notes) ?? studentProfile.Notes;
                studentProfile.CurrentLevel = ParseInitialLevel(request.Level);
            }
        }

        var link = await _dbContext.TeacherStudents.FirstOrDefaultAsync(
            item => item.SchoolId == schoolId
                && item.TeacherProfileId == teacherProfileId
                && item.StudentProfileId == studentProfile.Id,
            cancellationToken);

        if (link is null)
        {
            _dbContext.TeacherStudents.Add(new TeacherStudent(schoolId, teacherProfileId, studentProfile.Id, DateTime.UtcNow));
        }
        else
        {
            link.Reactivate();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var rows = await QueryBaseStudents(schoolId, teacherProfileId, cancellationToken);
        var all = await EnrichWithRepertoireAsync(rows, schoolId, cancellationToken);
        return all.First(item => item.StudentProfileId == studentProfile.Id);
    }

    public async Task<TeacherDashboardSummary> GetTeacherDashboardAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var rows = await QueryBaseStudents(schoolId, teacherProfileId, cancellationToken);
        var enriched = await EnrichWithRepertoireAsync(rows, schoolId, cancellationToken);
        var students = enriched.OrderBy(s => s.DisplayName).Take(4).ToArray();

        var activeStudentCount = await _dbContext.TeacherStudents.CountAsync(
            link => link.SchoolId == schoolId && link.TeacherProfileId == teacherProfileId && link.IsActive,
            cancellationToken);

        var pendingAssignmentCount = await _dbContext.Assignments.CountAsync(
            assignment => assignment.SchoolId == schoolId
                && assignment.TeacherProfileId == teacherProfileId
                && assignment.Status != AssignmentStatus.Completed
                && assignment.Status != AssignmentStatus.Skipped,
            cancellationToken);

        var recentCompletedAssignmentCount = await _dbContext.Assignments.CountAsync(
            assignment => assignment.SchoolId == schoolId
                && assignment.TeacherProfileId == teacherProfileId
                && assignment.Status == AssignmentStatus.Completed
                && assignment.CompletedAtUtc >= DateTime.UtcNow.AddDays(-14),
            cancellationToken);

        var recentLessons = await _dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.SchoolId == schoolId && lesson.TeacherProfileId == teacherProfileId)
            .OrderByDescending(lesson => lesson.LessonDateUtc)
            .Take(4)
            .Select(lesson => new LessonSummary(
                lesson.Id,
                lesson.StudentProfileId,
                lesson.Title,
                lesson.LessonDateUtc,
                lesson.CompletedAtUtc,
                lesson.Status,
                lesson.Notes))
            .ToArrayAsync(cancellationToken);

        return new TeacherDashboardSummary(
            activeStudentCount,
            pendingAssignmentCount,
            recentCompletedAssignmentCount,
            students,
            recentLessons);
    }

    private sealed record BaseStudentRow(
        Guid StudentProfileId,
        Guid SchoolUserId,
        string DisplayName,
        string Email,
        string Instrument,
        string Level,
        string Notes,
        int CurrentXp,
        int CurrentLevel);

    private async Task<List<BaseStudentRow>> QueryBaseStudents(
        Guid schoolId, Guid teacherProfileId, CancellationToken ct)
    {
        return await (
            from link in _dbContext.TeacherStudents.AsNoTracking()
            join student in _dbContext.StudentProfiles.AsNoTracking() on link.StudentProfileId equals student.Id
            join user in _dbContext.SchoolUsers.AsNoTracking() on student.SchoolUserId equals user.Id
            where link.SchoolId == schoolId
                && link.TeacherProfileId == teacherProfileId
                && link.IsActive
                && user.IsActive
            select new BaseStudentRow(
                student.Id,
                user.Id,
                user.DisplayName,
                user.Email,
                student.Instrument,
                student.Level,
                student.Notes,
                student.CurrentXp,
                student.CurrentLevel))
            .ToListAsync(ct);
    }

    private async Task<List<TeacherStudentSummary>> EnrichWithRepertoireAsync(
        List<BaseStudentRow> rows, Guid schoolId, CancellationToken ct)
    {
        if (rows.Count == 0)
            return [];

        var studentIds = rows.Select(r => r.StudentProfileId).ToList();

        var latestRepertoire = await _dbContext.RepertoireItems
            .AsNoTracking()
            .Where(item => item.SchoolId == schoolId && studentIds.Contains(item.StudentProfileId))
            .GroupBy(item => item.StudentProfileId)
            .Select(g => new
            {
                StudentProfileId = g.Key,
                Progress = g.OrderByDescending(x => x.UpdatedAtUtc).Select(x => x.ProgressPercent).FirstOrDefault(),
                Title = g.OrderByDescending(x => x.UpdatedAtUtc).Select(x => x.Title).FirstOrDefault()
            })
            .ToListAsync(ct);

        var repertoireByStudent = latestRepertoire.ToDictionary(r => r.StudentProfileId);

        return rows.Select(row =>
        {
            repertoireByStudent.TryGetValue(row.StudentProfileId, out var rep);
            return new TeacherStudentSummary(
                row.StudentProfileId,
                row.SchoolUserId,
                row.DisplayName,
                row.Email,
                row.Instrument,
                row.Level,
                row.Notes,
                row.CurrentXp,
                row.CurrentLevel,
                rep?.Progress ?? 0,
                rep?.Title);
        }).ToList();
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value.Trim();
    }

    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeEmail(string email) => RequireText(email, nameof(email)).ToUpperInvariant();

    private static string BuildAdministrativeEmail() => $"student-{Guid.NewGuid():N}@local.yourrhythm.internal".ToUpperInvariant();

    private static bool LooksLikeEmail(string? value) => !string.IsNullOrWhiteSpace(value) && value.Contains('@');

    private static bool IsEmail(string? value)
    {
        if (!LooksLikeEmail(value))
            return false;

        try
        {
            _ = new MailAddress(value!.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static int ParseInitialLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return 1;

        var digits = new string(level.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var parsed))
            return Math.Clamp(parsed, 1, 5);

        return 1;
    }
}
