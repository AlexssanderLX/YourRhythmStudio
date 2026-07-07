using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

internal static class LearningAuthorization
{
    public static (Guid SchoolId, Guid TeacherProfileId) RequireTeacher(AuthenticatedUserProfile profile)
    {
        if (profile.Role != YourRhythmRoles.Teacher || profile.SchoolId is null || profile.TeacherProfileId is null)
        {
            throw new UnauthorizedAccessException("Teacher profile is required.");
        }

        return (profile.SchoolId.Value, profile.TeacherProfileId.Value);
    }

    public static (Guid SchoolId, Guid StudentProfileId) RequireStudent(AuthenticatedUserProfile profile)
    {
        if (profile.Role != YourRhythmRoles.Student || profile.SchoolId is null || profile.StudentProfileId is null)
        {
            throw new UnauthorizedAccessException("Student profile is required.");
        }

        return (profile.SchoolId.Value, profile.StudentProfileId.Value);
    }

    public static async Task EnsureTeacherCanAccessStudentAsync(
        YourRhythmDbContext dbContext,
        Guid schoolId,
        Guid teacherProfileId,
        Guid studentProfileId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.TeacherStudents.AnyAsync(
            link => link.SchoolId == schoolId
                && link.TeacherProfileId == teacherProfileId
                && link.StudentProfileId == studentProfileId
                && link.IsActive,
            cancellationToken);

        if (!exists)
        {
            throw new UnauthorizedAccessException("Teacher cannot access this student.");
        }
    }
}

