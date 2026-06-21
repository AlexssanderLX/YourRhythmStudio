using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Users;

public sealed class UserDirectoryService : IUserDirectoryService
{
    private readonly YourRhythmDbContext _dbContext;

    public UserDirectoryService(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SchoolSummary> CreateSchoolAsync(
        CreateSchoolRequest request,
        CancellationToken cancellationToken = default)
    {
        var school = new School
        {
            Name = NormalizeRequired(request.Name, nameof(request.Name)),
            Slug = Slugify(request.Name),
            PrimaryEmail = NormalizeEmail(request.PrimaryEmail),
            OwnerAccountId = request.OwnerAccountId
        };

        _dbContext.Schools.Add(school);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SchoolSummary(school.Id, school.Name, school.Slug, school.PrimaryEmail, school.IsActive, 0, 0);
    }

    public async Task<TeacherSummary> CreateTeacherAsync(
        CreateTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchoolExistsAsync(request.SchoolId, cancellationToken);

        var user = new SchoolUser
        {
            SchoolId = request.SchoolId,
            AccountId = request.AccountId,
            DisplayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName)),
            Email = NormalizeEmail(request.Email),
            Role = YourRhythmRoles.Teacher
        };

        var teacher = new TeacherProfile
        {
            SchoolId = request.SchoolId,
            SchoolUserId = user.Id,
            InstrumentFocus = request.InstrumentFocus.Trim(),
            Bio = request.Bio.Trim()
        };

        _dbContext.SchoolUsers.Add(user);
        _dbContext.TeacherProfiles.Add(teacher);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TeacherSummary(teacher.Id, user.Id, user.DisplayName, user.Email, teacher.InstrumentFocus, user.IsActive);
    }

    public async Task<StudentSummary> CreateStudentAsync(
        CreateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchoolExistsAsync(request.SchoolId, cancellationToken);

        var user = new SchoolUser
        {
            SchoolId = request.SchoolId,
            AccountId = request.AccountId,
            DisplayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName)),
            Email = NormalizeEmail(request.Email),
            Role = YourRhythmRoles.Student
        };

        var student = new StudentProfile
        {
            SchoolId = request.SchoolId,
            SchoolUserId = user.Id,
            Instrument = request.Instrument.Trim(),
            Level = request.Level.Trim(),
            Notes = request.Notes.Trim()
        };

        _dbContext.SchoolUsers.Add(user);
        _dbContext.StudentProfiles.Add(student);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StudentSummary(
            student.Id,
            user.Id,
            user.DisplayName,
            user.Email,
            student.Instrument,
            student.Level,
            student.CurrentXp,
            student.CurrentLevel,
            user.IsActive);
    }

    public async Task<IReadOnlyCollection<SchoolSummary>> ListSchoolsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Schools
            .AsNoTracking()
            .OrderBy(school => school.Name)
            .Select(school => new SchoolSummary(
                school.Id,
                school.Name,
                school.Slug,
                school.PrimaryEmail,
                school.IsActive,
                school.Teachers.Count,
                school.Students.Count))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeacherSummary>> ListTeachersAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeacherProfiles
            .AsNoTracking()
            .Where(teacher => teacher.SchoolId == schoolId)
            .OrderBy(teacher => teacher.SchoolUser!.DisplayName)
            .Select(teacher => new TeacherSummary(
                teacher.Id,
                teacher.SchoolUserId,
                teacher.SchoolUser!.DisplayName,
                teacher.SchoolUser.Email,
                teacher.InstrumentFocus,
                teacher.SchoolUser.IsActive))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StudentSummary>> ListStudentsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StudentProfiles
            .AsNoTracking()
            .Where(student => student.SchoolId == schoolId)
            .OrderBy(student => student.SchoolUser!.DisplayName)
            .Select(student => new StudentSummary(
                student.Id,
                student.SchoolUserId,
                student.SchoolUser!.DisplayName,
                student.SchoolUser.Email,
                student.Instrument,
                student.Level,
                student.CurrentXp,
                student.CurrentLevel,
                student.SchoolUser.IsActive))
            .ToArrayAsync(cancellationToken);
    }

    private async Task EnsureSchoolExistsAsync(Guid schoolId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Schools.AnyAsync(
            school => school.Id == schoolId && school.IsActive,
            cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("School was not found or is inactive.");
        }
    }

    private static string NormalizeRequired(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        return NormalizeRequired(email, nameof(email)).Trim().ToUpperInvariant();
    }

    private static string Slugify(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value)).ToLowerInvariant();
        var builder = new System.Text.StringBuilder(normalized.Length);
        var previousHyphen = false;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousHyphen = false;
                continue;
            }

            if (!previousHyphen)
            {
                builder.Append('-');
                previousHyphen = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
