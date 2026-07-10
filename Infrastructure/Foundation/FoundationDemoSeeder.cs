using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Security;
using Foundation.Access.Services;
using Foundation.Core.Abstractions;
using Foundation.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Infrastructure.Foundation;

public static class FoundationDemoSeeder
{
    public static async Task SeedFoundationDemoAccountAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var accessService = scope.ServiceProvider.GetRequiredService<SaasAccessService>();

        // Admin da plataforma (fluxo oficial da Foundation).
        var result = await accessService.CreatePlatformAdministratorAsync(
            new CreatePlatformAdministratorRequest(
                "Admin YourRhythm",
                "admin@yourrhythm.local",
                DemoPersonas.DemoPassword));

        if (result.IsFailure &&
            result.Error?.Code is not ("conflict" or "unauthorized"))
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Nao foi possivel criar o usuario demo.");
        }

        // Contas demo (escola / professor / aluno) para visualizar cada dashboard.
        // Semeadas direto no store in-memory: são contas comuns (PlatformRole = None)
        // e o papel de domínio é resolvido pelo e-mail em DemoPersonas.
        var accountStore = scope.ServiceProvider.GetRequiredService<IAccountStore>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var dbContext = scope.ServiceProvider.GetRequiredService<YourRhythmDbContext>();

        await dbContext.Database.MigrateAsync();

        foreach (var persona in DemoPersonas.All)
        {
            var account = await accountStore.FindByEmailAsync(persona.Email);
            if (account is not null)
            {
                await UpsertYourRhythmDemoProfileAsync(dbContext, account, persona, clock.UtcNow);
                continue;
            }

            var now = clock.UtcNow;
            account = new Account
            {
                DisplayName = persona.DisplayName,
                Email = persona.Email.ToUpperInvariant(),
                Status = AccountStatus.Active,
                PlatformRole = PlatformAccessRole.None,
                PasswordCredential = passwordHasher.HashPassword(DemoPersonas.DemoPassword),
                CreatedAtUtc = now,
                ActivatedAtUtc = now,
                SecurityStamp = SecureCodeGenerator.GenerateToken(16)
            };

            await accountStore.SaveAsync(account);
            await UpsertYourRhythmDemoProfileAsync(dbContext, account, persona, now);
        }

        await SeedLearningDemoAsync(dbContext, clock.UtcNow);
    }

    private static async Task UpsertYourRhythmDemoProfileAsync(
        YourRhythmDbContext dbContext,
        Account account,
        DemoPersona persona,
        DateTime utcNow)
    {
        if (persona.Role == YourRhythmRoles.SchoolOwner)
        {
            return;
        }

        var school = await dbContext.Schools.FirstOrDefaultAsync(school => school.Slug == DemoPersonas.SchoolSlug);
        if (school is null)
        {
            school = new School
            {
                Name = "Escola Harmonia",
                Slug = DemoPersonas.SchoolSlug,
                PrimaryEmail = "escola@yourrhythm.local",
                OwnerAccountId = null
            };
            dbContext.Schools.Add(school);
            await dbContext.SaveChangesAsync();
        }

        var normalizedEmail = persona.Email.ToUpperInvariant();
        var schoolUser = await dbContext.SchoolUsers.FirstOrDefaultAsync(
            user => user.SchoolId == school.Id && user.Email == normalizedEmail);

        if (schoolUser is null)
        {
            schoolUser = new SchoolUser
            {
                SchoolId = school.Id,
                AccountId = account.Id,
                DisplayName = persona.DisplayName,
                Email = normalizedEmail,
                Role = persona.Role
            };
            dbContext.SchoolUsers.Add(schoolUser);
        }
        else
        {
            schoolUser.AccountId = account.Id;
            schoolUser.DisplayName = persona.DisplayName;
            schoolUser.Role = persona.Role;
        }

        await dbContext.SaveChangesAsync();

        if (persona.Role == YourRhythmRoles.Teacher)
        {
            var teacherExists = await dbContext.TeacherProfiles.AnyAsync(profile => profile.SchoolUserId == schoolUser.Id);
            if (!teacherExists)
            {
                dbContext.TeacherProfiles.Add(new TeacherProfile
                {
                    SchoolId = school.Id,
                    SchoolUserId = schoolUser.Id,
                    InstrumentFocus = "Piano",
                    Bio = "Professora demo do MVP YourRhythm."
                });
            }
        }

        if (persona.Role == YourRhythmRoles.Student)
        {
            var studentExists = await dbContext.StudentProfiles.AnyAsync(profile => profile.SchoolUserId == schoolUser.Id);
            if (!studentExists)
            {
                dbContext.StudentProfiles.Add(new StudentProfile
                {
                    SchoolId = school.Id,
                    SchoolUserId = schoolUser.Id,
                    Instrument = "Piano",
                    Level = "Iniciante",
                    Notes = "Aluno demo do MVP.",
                    CurrentXp = 120,
                    CurrentLevel = 1
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedLearningDemoAsync(YourRhythmDbContext dbContext, DateTime utcNow)
    {
        var school = await dbContext.Schools.FirstAsync(school => school.Slug == DemoPersonas.SchoolSlug);
        var teacher = await dbContext.TeacherProfiles.FirstAsync(profile => profile.SchoolId == school.Id);
        var student = await dbContext.StudentProfiles.FirstAsync(profile => profile.SchoolId == school.Id);

        if (!await dbContext.TeacherStudents.AnyAsync(link =>
            link.SchoolId == school.Id &&
            link.TeacherProfileId == teacher.Id &&
            link.StudentProfileId == student.Id))
        {
            dbContext.TeacherStudents.Add(new TeacherStudent(school.Id, teacher.Id, student.Id, utcNow));
        }

        if (!await dbContext.RepertoireItems.AnyAsync(item => item.SchoolId == school.Id && item.StudentProfileId == student.Id))
        {
            var repertoire = new RepertoireItem(
                school.Id,
                teacher.Id,
                student.Id,
                "Clair de Lune",
                utcNow);
            repertoire.UpdateDetails(
                repertoire.Title,
                "Ouça a referência e marque como concluída depois do estudo.",
                "https://www.youtube.com/results?search_query=clair+de+lune",
                utcNow);
            repertoire.UpdateProgress(40, utcNow);
            dbContext.RepertoireItems.Add(repertoire);
        }

        if (!await dbContext.Assignments.AnyAsync(item => item.SchoolId == school.Id && item.StudentProfileId == student.Id))
        {
            var assignment = new Assignment(
                school.Id,
                teacher.Id,
                student.Id,
                "Praticar escalas maiores",
                "Pratique 20 minutos de escalas maiores antes da proxima aula.",
                utcNow.AddDays(7),
                80,
                utcNow);
            assignment.UpdateDetails(
                assignment.Title,
                assignment.Description,
                assignment.DueAtUtc,
                assignment.XpReward,
                utcNow);
            dbContext.Assignments.Add(assignment);
        }

        if (!await dbContext.FeedbackEntries.AnyAsync(item => item.SchoolId == school.Id && item.StudentProfileId == student.Id))
        {
            dbContext.FeedbackEntries.Add(new FeedbackEntry(
                school.Id,
                teacher.Id,
                student.Id,
                "Boa evolucao no controle do tempo. Continue praticando devagar antes de acelerar.",
                true,
                utcNow));
        }

        if (!await dbContext.XpEvents.AnyAsync(item => item.SchoolId == school.Id && item.StudentProfileId == student.Id))
        {
            dbContext.XpEvents.Add(new XpEvent(
                school.Id,
                student.Id,
                XpEventType.ManualAdjustment,
                120,
                "XP inicial demo",
                utcNow,
                teacher.Id));
        }

        await dbContext.SaveChangesAsync();
    }
}
