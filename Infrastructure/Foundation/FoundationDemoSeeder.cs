using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Infrastructure.Foundation;

public static class FoundationDemoSeeder
{
    private const string LegacyDemoSchoolSlug = "escola-harmonia";

    private static readonly string[] LegacyDemoEmails =
    [
        "ESCOLA@YOURRHYTHM.LOCAL",
        "PROFESSOR@YOURRHYTHM.LOCAL",
        "ALUNO@YOURRHYTHM.LOCAL"
    ];

    public static async Task RemoveFoundationDemoAccountsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<YourRhythmDbContext>();

        await dbContext.Database.MigrateAsync();

        var legacySchool = await dbContext.Schools
            .FirstOrDefaultAsync(school => school.Slug == LegacyDemoSchoolSlug);

        if (legacySchool is not null)
        {
            var schoolId = legacySchool.Id;

            dbContext.FeedbackEntries.RemoveRange(await dbContext.FeedbackEntries.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.XpEvents.RemoveRange(await dbContext.XpEvents.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.StudentSkillMasteries.RemoveRange(await dbContext.StudentSkillMasteries.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.Assignments.RemoveRange(await dbContext.Assignments.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.RepertoireItems.RemoveRange(await dbContext.RepertoireItems.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.Lessons.RemoveRange(await dbContext.Lessons.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.LevelUpEvents.RemoveRange(await dbContext.LevelUpEvents.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.LevelConfigs.RemoveRange(await dbContext.LevelConfigs.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.Skills.RemoveRange(await dbContext.Skills.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.TeacherStudents.RemoveRange(await dbContext.TeacherStudents.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.StudentProfiles.RemoveRange(await dbContext.StudentProfiles.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.TeacherProfiles.RemoveRange(await dbContext.TeacherProfiles.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.SchoolUsers.RemoveRange(await dbContext.SchoolUsers.Where(item => item.SchoolId == schoolId).ToListAsync());
            dbContext.Schools.Remove(legacySchool);
        }

        foreach (var legacyEmail in LegacyDemoEmails)
        {
            var legacyAccount = await dbContext.PersistedAccounts
                .FirstOrDefaultAsync(account => account.Email == legacyEmail);
            if (legacyAccount is not null)
            {
                dbContext.PersistedAccounts.Remove(legacyAccount);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
